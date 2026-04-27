using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Options;

namespace PathfinderRagChatUi.Services;

public sealed class AppQueryService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IOptions<CorpusOptions> corpusOptions)
{
    public async Task<CorpusSummaryDto> GetSummaryAsync(string? corpusName, CancellationToken cancellationToken)
    {
        var resolvedCorpusName = ResolveCorpusName(corpusName);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var snapshot = (await db.CorpusSnapshots
                .Where(x => x.CorpusName == resolvedCorpusName)
                .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.UpdatedUtc)
            .FirstOrDefault();

        if (snapshot is null)
        {
            return new CorpusSummaryDto(
                resolvedCorpusName,
                corpusOptions.Value.RepositoryUrl,
                corpusOptions.Value.Branch,
                null,
                corpusOptions.Value.WorkingDirectory,
                0,
                0,
                DateTimeOffset.MinValue,
                string.Empty,
                string.Empty);
        }

        return new CorpusSummaryDto(
            snapshot.CorpusName,
            snapshot.RepositoryUrl,
            snapshot.Branch,
            snapshot.CommitSha,
            snapshot.CheckoutRoot,
            snapshot.FileCount,
            snapshot.ChunkCount,
            snapshot.UpdatedUtc,
            snapshot.EmbeddingModel,
            snapshot.ChatModel);
    }

    public async Task<IReadOnlyList<HistoryItemDto>> GetHistoryAsync(string? corpusName, int limit, CancellationToken cancellationToken)
    {
        var resolvedCorpusName = ResolveCorpusName(corpusName);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.ChatRecords
            .Where(x => x.CorpusName == resolvedCorpusName)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(x => x.CreatedUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(MapHistory)
            .ToList();
    }

    public async Task<IReadOnlyList<PinDto>> GetPinsAsync(string? corpusName, int limit, CancellationToken cancellationToken)
    {
        var resolvedCorpusName = ResolveCorpusName(corpusName);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.PinRecords
            .Where(x => x.CorpusName == resolvedCorpusName)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(x => x.CreatedUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(MapPin)
            .ToList();
    }

    public async Task<SourceChunkDto> GetChunkAsync(string corpusName, Guid chunkId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.CorpusChunks
            .Where(x => x.CorpusName == corpusName && x.Id == chunkId)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            throw new InvalidOperationException("Chunk not found.");
        }

        return new SourceChunkDto(row.CorpusName, row.Id, row.SourcePath, row.SourceTitle, row.ChunkIndex, row.Text);
    }

    public async Task<PinDto> PinAsync(string? corpusName, PinRequestDto request, CancellationToken cancellationToken)
    {
        var resolvedCorpusName = ResolveCorpusName(corpusName);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var record = await db.ChatRecords
            .Where(x => x.CorpusName == resolvedCorpusName && x.Id == request.RecordId)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("record_id not found in history.");
        }

        var pin = new PinRecord
        {
            ChatRecordId = record.Id,
            CorpusName = record.CorpusName,
            CreatedUtc = DateTimeOffset.UtcNow,
            Note = request.Note ?? string.Empty,
            Topic = record.Topic,
            Question = record.Question,
            Answer = record.Answer,
            CitationsJson = record.CitationsJson
        };

        db.PinRecords.Add(pin);
        await db.SaveChangesAsync(cancellationToken);

        return MapPin(pin);
    }

    private string ResolveCorpusName(string? corpusName) => string.IsNullOrWhiteSpace(corpusName) ? corpusOptions.Value.Name : corpusName!;

    private static HistoryItemDto MapHistory(ChatRecord record)
        => new(
            record.Id,
            record.CorpusName,
            record.CreatedUtc,
            record.Topic,
            record.Question,
            record.Answer,
            record.Model,
            DeserializeCitations(record.CitationsJson));

    private static PinDto MapPin(PinRecord record)
        => new(
            record.Id,
            record.ChatRecordId,
            record.CorpusName,
            record.CreatedUtc,
            record.Topic,
            record.Question,
            record.Answer,
            record.Note,
            DeserializeCitations(record.CitationsJson));

    private static IReadOnlyList<CitationDto> DeserializeCitations(string json)
        => JsonSerializer.Deserialize<List<CitationDto>>(json) ?? [];
}
