using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Services;

namespace PathfinderRagChatUi.Tests;

public sealed class CorpusRefreshAndRagTests
{
    [Fact]
    public async Task Refresh_And_Chat_UseLatestMarkdownFromLocalCorpus()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"pf2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);

        var persistentDamagePath = Path.Combine(sourceRoot, "rules", "persistent-damage.md");
        Directory.CreateDirectory(Path.GetDirectoryName(persistentDamagePath)!);
        await File.WriteAllTextAsync(persistentDamagePath, """
            # Persistent Damage

            At the end of each of your turns, you take the listed damage.
            Your persistent damage ends when you succeed at the flat check.
            """);

        var grapplingPath = Path.Combine(sourceRoot, "rules", "grappling.md");
        await File.WriteAllTextAsync(grapplingPath, """
            # Grappling

            This document talks about grabbing, restraining, and moving foes.
            """);

        var fakeGit = new FakeGitRepositoryClient(sourceRoot);
        var fakeOllama = new FakeOllamaClient();
        var (provider, dbPath) = TestHelpers.BuildProvider(sourceRoot, fakeOllama, fakeGit, includeRoots: ["rules"]);

        try
        {
            using var scope = provider.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<CorpusRefreshService>();
            var chat = scope.ServiceProvider.GetRequiredService<ChatService>();
            var queries = scope.ServiceProvider.GetRequiredService<AppQueryService>();

            var result = await refresh.RefreshAsync(null, CancellationToken.None);
            Assert.Equal(2, result.FileCount);

            var preparation = await chat.PrepareAsync(new ChatRequestDto("How does persistent damage work?", 3), CancellationToken.None);
            Assert.NotEmpty(preparation.Citations);
            Assert.Contains(preparation.Citations, citation => citation.SourcePath.Contains("persistent-damage.md", StringComparison.OrdinalIgnoreCase));

            var response = await chat.ChatAsync(new ChatRequestDto("How does persistent damage work?", 3, "test-chat"), CancellationToken.None);
            Assert.NotEqual(Guid.Empty, response.RecordId);
            Assert.Contains("Answer generated", response.Answer);

            var history = await queries.GetHistoryAsync("pf2e-srd", 20, CancellationToken.None);
            Assert.Single(history);
            Assert.Equal("How does persistent damage work?", history[0].Question);

            await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
            Assert.Equal(2, await db.CorpusChunks.CountAsync());
        }
        finally
        {
            await TestHelpers.DisposeProviderAsync(provider, dbPath);
            if (Directory.Exists(sourceRoot))
            {
                Directory.Delete(sourceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Refresh_Can_Opt_Out_Of_Fantasy_Bestiary()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"pf2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);

        var rulesPath = Path.Combine(sourceRoot, "rules", "persistent-damage.md");
        Directory.CreateDirectory(Path.GetDirectoryName(rulesPath)!);
        await File.WriteAllTextAsync(rulesPath, """
            # Persistent Damage

            This rules page should stay in the selective import.
            """);

        var bestiaryPath = Path.Combine(sourceRoot, "fantasy-bestiary", "goblin.md");
        Directory.CreateDirectory(Path.GetDirectoryName(bestiaryPath)!);
        await File.WriteAllTextAsync(bestiaryPath, """
            # Goblin

            This enemy page should be excluded when bestiary is opted out.
            """);

        var fakeGit = new FakeGitRepositoryClient(sourceRoot);
        var fakeOllama = new FakeOllamaClient();
        var (provider, dbPath) = TestHelpers.BuildProvider(
            sourceRoot,
            fakeOllama,
            fakeGit,
            includeRoots: ["rules", "fantasy-bestiary"]);

        try
        {
            using var scope = provider.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<CorpusRefreshService>();

            var result = await refresh.RefreshAsync(
                new CorpusRefreshRequestDto(false, ["rules", "fantasy-bestiary"], ["fantasy-bestiary"]),
                CancellationToken.None);

            Assert.Equal(1, result.FileCount);

            await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
            var sourcePaths = await db.CorpusChunks.Select(x => x.SourcePath).ToListAsync();
            Assert.NotEmpty(sourcePaths);
            Assert.All(sourcePaths, path => Assert.Contains("persistent-damage.md", path, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(sourcePaths, path => path.Contains("goblin.md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await TestHelpers.DisposeProviderAsync(provider, dbPath);
            if (Directory.Exists(sourceRoot))
            {
                Directory.Delete(sourceRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Live_Companion_Repo_Rag_Path_Requires_OptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_CORPUS_TESTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://192.168.0.252:11434";
        var sourceRoot = Path.Combine(Path.GetTempPath(), $"pf2e-live-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceRoot);

        var realOllama = new OllamaClient(new HttpClient { BaseAddress = new Uri(ollamaUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(30) });
        var availableModels = await realOllama.GetModelsAsync(CancellationToken.None);
        if (availableModels.Count == 0)
        {
            throw new InvalidOperationException($"No Ollama models were returned from {ollamaUrl}.");
        }

        var selectedModel = availableModels.FirstOrDefault(model => !model.Contains("embed", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No chat-capable Ollama model was returned from {ollamaUrl}. Models: {string.Join(", ", availableModels)}");
        var fakeGit = new GitRepositoryClient();
        var (provider, dbPath) = TestHelpers.BuildProvider(sourceRoot, realOllama, fakeGit,
            repositoryUrl: "https://github.com/Obsidian-TTRPG-Community/Pathfinder-2E-SRD-Markdown",
            includeRoots: ["rules/actions/grapple.md"],
            chatModel: selectedModel);

        try
        {
            using var scope = provider.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<CorpusRefreshService>();
            var chat = scope.ServiceProvider.GetRequiredService<ChatService>();

            var result = await refresh.RefreshAsync(null, CancellationToken.None);
            Assert.Equal(1, result.FileCount);
            Assert.True(result.ChunkCount > 0);

            var response = await chat.ChatAsync(new ChatRequestDto("What does Grapple do?", 5, selectedModel), CancellationToken.None);
            Assert.NotEqual(Guid.Empty, response.RecordId);
            Assert.NotEmpty(response.Citations);
            Assert.NotEmpty(response.Answer);
        }
        finally
        {
            await TestHelpers.DisposeProviderAsync(provider, dbPath);
            if (Directory.Exists(sourceRoot))
            {
                Directory.Delete(sourceRoot, recursive: true);
            }
        }
    }
}
