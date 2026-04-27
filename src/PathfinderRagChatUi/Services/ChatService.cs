using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Options;

namespace PathfinderRagChatUi.Services;

public sealed class ChatService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IOllamaClient ollamaClient,
    IOptions<CorpusOptions> corpusOptions,
    IOptions<OllamaOptions> ollamaOptions)
{
    public async Task<ChatPreparationDto> PrepareAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        var corpusName = ResolveCorpusName(request.CorpusName);
        var topK = Math.Clamp(request.TopK, 1, 12);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var chunks = await db.CorpusChunks
            .Where(x => x.CorpusName == corpusName)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException($"No corpus chunks were found for '{corpusName}'. Refresh the database first.");
        }

        var queryEmbedding = (await ollamaClient.EmbedAsync([request.Question], ollamaOptions.Value.EmbeddingModel, cancellationToken)).Single();
        var scored = chunks
            .Select(chunk =>
            {
                var embedding = DeserializeEmbedding(chunk.EmbeddingJson);
                var semantic = CosineSimilarity(queryEmbedding, embedding);
                var keyword = KeywordOverlapScore(request.Question, chunk.Text, chunk.SourcePath);
                var score = (semantic * 0.85) + (keyword * 0.15);
                return (chunk, score);
            })
            .OrderByDescending(x => x.score)
            .Take(topK)
            .ToList();

        var citations = scored.Select((item, index) =>
        {
            var chunk = item.chunk;
            return new CitationDto(
                index + 1,
                Math.Round(item.score, 4),
                chunk.Id,
                chunk.ChunkIndex,
                chunk.SourcePath,
                chunk.SourceTitle,
                chunk.Text.Length > 350 ? chunk.Text[..350] : chunk.Text);
        }).ToList();

        var context = new StringBuilder();
        for (var i = 0; i < scored.Count; i++)
        {
            var chunk = scored[i].chunk;
            context.AppendLine($"[Source {i + 1}] {chunk.SourceTitle} ({chunk.SourcePath})");
            context.AppendLine(chunk.Text);
            context.AppendLine();
        }

        var system = """
            You are The Dungeon Master: confident, immersive, and concise.
            Use a flavorful tabletop DM voice, but remain mechanically accurate.
            Prioritize exact rule text from provided context and avoid invention.
            If context is insufficient or conflicting, explicitly say so.
            Do not claim certainty beyond cited sources.
            """;

        var prompt = $"""
            Context:
            {context}

            Question:
            {request.Question}

            Response format:
            1) Open with a short DM-style ruling statement.
            2) Quote the most relevant rule text with [Source N].
            3) Explain practical table use in plain language.
            4) If uncertain or conflicting, state uncertainty clearly.
            """;

        return new ChatPreparationDto(corpusName, system, prompt, citations);
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        ChatPreparationDto preparation,
        string? model,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatModel = string.IsNullOrWhiteSpace(model) ? ollamaOptions.Value.ChatModel : model;
        await foreach (var token in ollamaClient.StreamGenerateAsync(preparation.SystemPrompt, preparation.Prompt, chatModel, cancellationToken))
        {
            yield return token;
        }
    }

    public async Task<ChatResponseDto> FinalizeAsync(
        ChatRequestDto request,
        ChatPreparationDto preparation,
        string answer,
        CancellationToken cancellationToken)
    {
        var corpusName = preparation.CorpusName;
        var chatModel = string.IsNullOrWhiteSpace(request.Model) ? ollamaOptions.Value.ChatModel : request.Model!;
        var topic = TopicFromQuestion(request.Question);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = new ChatRecord
        {
            CorpusName = corpusName,
            CreatedUtc = DateTimeOffset.UtcNow,
            Topic = topic,
            Question = request.Question,
            Answer = answer,
            Model = chatModel,
            CitationsJson = JsonSerializer.Serialize(preparation.Citations)
        };

        db.ChatRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto(
            record.Id,
            corpusName,
            request.Question,
            answer,
            preparation.Citations,
            chatModel,
            topic);
    }

    public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        var preparation = await PrepareAsync(request, cancellationToken);
        var answer = await ollamaClient.GenerateAsync(preparation.SystemPrompt, preparation.Prompt, string.IsNullOrWhiteSpace(request.Model) ? ollamaOptions.Value.ChatModel : request.Model!, cancellationToken);
        return await FinalizeAsync(request, preparation, answer, cancellationToken);
    }

    private string ResolveCorpusName(string? corpusName) => string.IsNullOrWhiteSpace(corpusName) ? corpusOptions.Value.Name : corpusName!;

    private static float[] DeserializeEmbedding(string value)
        => JsonSerializer.Deserialize<float[]>(value) ?? [];

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var count = Math.Min(left.Count, right.Count);
        if (count == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var i = 0; i < count; i++)
        {
            var l = left[i];
            var r = right[i];
            dot += l * r;
            leftMagnitude += l * l;
            rightMagnitude += r * r;
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static double KeywordOverlapScore(string query, string text, string sourcePath)
    {
        var terms = Regex.Matches(query.ToLowerInvariant(), @"\b[a-z0-9]{4,}\b")
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (terms.Count == 0)
        {
            return 0;
        }

        var haystack = $"{sourcePath} {text[..Math.Min(600, text.Length)]}".ToLowerInvariant();
        var hits = terms.Count(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
        return Math.Min(1.0, hits / (double)terms.Count);
    }

    private static string TopicFromQuestion(string question)
    {
        var trimmed = question.Trim().ReplaceLineEndings(" ");
        return trimmed.Length <= 96 ? trimmed : trimmed[..96];
    }
}
