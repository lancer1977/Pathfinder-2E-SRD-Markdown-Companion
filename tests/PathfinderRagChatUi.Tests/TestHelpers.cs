using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Options;
using PathfinderRagChatUi.Services;

namespace PathfinderRagChatUi.Tests;

internal static class TestHelpers
{
    public static (ServiceProvider Provider, string DbPath) BuildProvider(
        string sourceRoot,
        IOllamaClient ollamaClient,
        IGitRepositoryClient gitRepositoryClient,
        string corpusName = "pf2e-srd",
        string? repositoryUrl = null,
        string? branch = null,
        IReadOnlyList<string>? includeRoots = null,
        IReadOnlyList<string>? excludeRoots = null,
        bool useWholeRepo = false,
        string? chatModel = null,
        int chunkSize = 800,
        int chunkOverlap = 140)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ollamaClient);
        services.AddSingleton(gitRepositoryClient);
        services.AddSingleton<MarkdownChunker>();
        services.AddSingleton<IOptions<CorpusOptions>>(Microsoft.Extensions.Options.Options.Create(new CorpusOptions
        {
            Name = corpusName,
            RepositoryUrl = repositoryUrl ?? "https://example.test/repo",
            Branch = branch ?? "main",
            WorkingDirectory = sourceRoot,
            UseWholeRepo = useWholeRepo,
            IncludeRoots = includeRoots is { Count: > 0 } ? [.. includeRoots] : ["compendium", "rules", "fantasy-bestiary"],
            ExcludeRoots = excludeRoots is { Count: > 0 } ? [.. excludeRoots] : [],
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap
        }));
        services.AddSingleton<IOptions<OllamaOptions>>(Microsoft.Extensions.Options.Options.Create(new OllamaOptions
        {
            BaseUrl = "http://127.0.0.1:11434",
            EmbeddingModel = "nomic-embed-text:latest",
            ChatModel = chatModel ?? "test-chat"
        }));
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        services.AddScoped<CorpusRefreshService>();
        services.AddScoped<ChatService>();
        services.AddScoped<AppQueryService>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();

        return (provider, dbPath);
    }

    public static async Task DisposeProviderAsync(ServiceProvider provider, string dbPath)
    {
        await provider.DisposeAsync();

        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}

internal sealed class FakeGitRepositoryClient(string sourceRoot, string commitSha = "test-sha") : IGitRepositoryClient
{
    public Task<string> CloneFreshAsync(string repositoryUrl, string branch, string destinationRoot, CancellationToken cancellationToken)
        => Task.FromResult(sourceRoot);

    public Task<string?> GetHeadCommitAsync(string repositoryPath, CancellationToken cancellationToken)
        => Task.FromResult<string?>(commitSha);
}

internal sealed class FakeOllamaClient : IOllamaClient
{
    public Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(["test-chat"]);

    public Task<IReadOnlyList<IReadOnlyList<float>>> EmbedAsync(IReadOnlyList<string> inputs, string model, CancellationToken cancellationToken)
    {
        var vectors = inputs.Select(CreateEmbedding).Select(x => (IReadOnlyList<float>)x).ToArray();
        return Task.FromResult<IReadOnlyList<IReadOnlyList<float>>>(vectors);
    }

    public Task<string> GenerateAsync(string system, string prompt, string model, CancellationToken cancellationToken)
        => Task.FromResult($"Answer generated for {model}.");

    public async IAsyncEnumerable<string> StreamGenerateAsync(string system, string prompt, string model, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await GenerateAsync(system, prompt, model, cancellationToken);
    }

    private static float[] CreateEmbedding(string input)
    {
        var sum = input.Sum(c => (int)c);
        return
        [
            input.Length / 1000f,
            sum / 100000f,
            input.Count(char.IsLetter) / 1000f,
            input.Count(char.IsWhiteSpace) / 1000f
        ];
    }
}
