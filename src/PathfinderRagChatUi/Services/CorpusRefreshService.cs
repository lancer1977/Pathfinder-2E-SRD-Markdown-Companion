using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Options;

namespace PathfinderRagChatUi.Services;

public sealed class CorpusRefreshService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IGitRepositoryClient gitRepositoryClient,
    MarkdownChunker chunker,
    IOllamaClient ollamaClient,
    IOptions<CorpusOptions> corpusOptions,
    IOptions<OllamaOptions> ollamaOptions,
    ILogger<CorpusRefreshService> logger)
{
    public async Task<RefreshResultDto> RefreshAsync(
        CorpusRefreshRequestDto? request,
        CancellationToken cancellationToken,
        IProgress<RefreshProgressDto>? progress = null)
    {
        var corpus = corpusOptions.Value;
        var ollama = ollamaOptions.Value;
        var corpusName = corpus.Name.Trim();
        var importSpec = ResolveImportSpec(corpus, request);

        progress?.Report(new RefreshProgressDto("clone", "Cloning a fresh checkout", 0, 0, 0, 0));
        var checkoutRoot = await gitRepositoryClient.CloneFreshAsync(
            corpus.RepositoryUrl,
            corpus.Branch,
            corpus.WorkingDirectory,
            cancellationToken);

        var commitSha = await gitRepositoryClient.GetHeadCommitAsync(checkoutRoot, cancellationToken);
        var files = FindMarkdownFiles(checkoutRoot, importSpec);

        progress?.Report(new RefreshProgressDto("scan", $"Found {files.Count} markdown files", 0, files.Count, 0, 0));

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var chunks = new List<CorpusChunk>();
        var fileCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            fileCount++;
            progress?.Report(new RefreshProgressDto("chunk", $"Chunking {Path.GetFileName(file)}", fileCount, files.Count, chunks.Count, 0));

            var text = await File.ReadAllTextAsync(file, Encoding.UTF8, cancellationToken);
            var relative = Path.GetRelativePath(checkoutRoot, file);
            var sourceTitle = MakeTitle(relative);
            var pieces = chunker.Chunk(text, corpus.ChunkSize, corpus.ChunkOverlap);

            for (var i = 0; i < pieces.Count; i++)
            {
                chunks.Add(new CorpusChunk
                {
                    CorpusName = corpusName,
                    SourcePath = relative,
                    SourceTitle = sourceTitle,
                    ChunkIndex = i,
                    Text = pieces[i]
                });
            }
        }

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("No chunkable markdown content was found in the source repository.");
        }

        progress?.Report(new RefreshProgressDto("embed", "Generating embeddings", files.Count, files.Count, 0, chunks.Count));

        db.CorpusChunks.RemoveRange(db.CorpusChunks.Where(x => x.CorpusName == corpusName));
        db.CorpusSnapshots.RemoveRange(db.CorpusSnapshots.Where(x => x.CorpusName == corpusName));
        await db.SaveChangesAsync(cancellationToken);

        var batchSize = 32;
        for (var index = 0; index < chunks.Count; index += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = chunks.Skip(index).Take(batchSize).ToList();
            var vectors = await ollamaClient.EmbedAsync(
                batch.Select(x => x.Text).ToList(),
                ollama.EmbeddingModel,
                cancellationToken);

            for (var i = 0; i < batch.Count; i++)
            {
                batch[i].EmbeddingJson = JsonSerializer.Serialize(vectors[i]);
            }

            db.CorpusChunks.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);

            progress?.Report(new RefreshProgressDto(
                "embed",
                $"Embedded {Math.Min(index + batch.Count, chunks.Count)} of {chunks.Count} chunks",
                files.Count,
                files.Count,
                Math.Min(index + batch.Count, chunks.Count),
                chunks.Count));
        }

        var snapshot = new CorpusSnapshot
        {
            CorpusName = corpusName,
            RepositoryUrl = corpus.RepositoryUrl,
            Branch = corpus.Branch,
            CommitSha = commitSha,
            CheckoutRoot = checkoutRoot,
            FileCount = files.Count,
            ChunkCount = chunks.Count,
            ChunkSize = corpus.ChunkSize,
            ChunkOverlap = corpus.ChunkOverlap,
            EmbeddingModel = ollama.EmbeddingModel,
            ChatModel = ollama.ChatModel,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        db.CorpusSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);

        progress?.Report(new RefreshProgressDto("done", "Refresh completed", files.Count, files.Count, chunks.Count, chunks.Count));

        logger.LogInformation(
            "Refreshed corpus {CorpusName} from {RepositoryUrl} with {FileCount} files and {ChunkCount} chunks",
            corpusName,
            corpus.RepositoryUrl,
            files.Count,
            chunks.Count);

        return new RefreshResultDto(
            corpusName,
            corpus.RepositoryUrl,
            corpus.Branch,
            commitSha,
            checkoutRoot,
            files.Count,
            chunks.Count,
            snapshot.UpdatedUtc);
    }

    private static ImportSpec ResolveImportSpec(CorpusOptions corpus, CorpusRefreshRequestDto? request)
    {
        var useWholeRepo = request?.UseWholeRepo ?? corpus.UseWholeRepo;
        if (useWholeRepo)
        {
            return new ImportSpec(true, [], []);
        }

        var includeRoots = (request?.IncludeRoots?.Count > 0 ? request.IncludeRoots : corpus.IncludeRoots)
            .Select(NormalizeRoot)
            .ToList();
        var excludeRoots = (request?.ExcludeRoots?.Count > 0 ? request.ExcludeRoots : corpus.ExcludeRoots)
            .Select(NormalizeRoot)
            .ToList();

        return new ImportSpec(false, includeRoots, excludeRoots);
    }

    private static List<string> FindMarkdownFiles(string root, ImportSpec importSpec)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        if (importSpec.UseWholeRepo)
        {
            matcher.AddInclude("**/*.md");
        }
        else
        {
            foreach (var include in importSpec.IncludeRoots)
            {
                matcher.AddInclude(ExpandPattern(include));
            }
        }

        foreach (var exclude in importSpec.ExcludeRoots)
        {
            matcher.AddExclude(ExpandPattern(exclude));
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(root)));
        return result.Files
            .Select(x => Path.GetFullPath(Path.Combine(root, x.Path)))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeRoot(string value) => value.Trim().Trim('/', '\\');

    private static string ExpandPattern(string value)
    {
        var normalized = NormalizeRoot(value);
        if (normalized.Contains('*') || normalized.Contains('?') || normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return $"{normalized}/**/*.md";
    }

    private static string MakeTitle(string relativePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var text = fileName.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }

    private sealed record ImportSpec(bool UseWholeRepo, IReadOnlyList<string> IncludeRoots, IReadOnlyList<string> ExcludeRoots);
}
