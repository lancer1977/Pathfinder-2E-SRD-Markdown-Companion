using Microsoft.EntityFrameworkCore;

namespace PathfinderRagChatUi.Data;

[Index(nameof(CorpusName), IsUnique = true)]
public sealed class CorpusSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CorpusName { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public string? CommitSha { get; set; }

    public string CheckoutRoot { get; set; } = string.Empty;

    public int FileCount { get; set; }

    public int ChunkCount { get; set; }

    public int ChunkSize { get; set; }

    public int ChunkOverlap { get; set; }

    public string EmbeddingModel { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;

    public DateTimeOffset UpdatedUtc { get; set; }
}

[Index(nameof(CorpusName), nameof(SourcePath), nameof(ChunkIndex))]
public sealed class CorpusChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CorpusName { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string SourceTitle { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string EmbeddingJson { get; set; } = string.Empty;
}

[Index(nameof(CorpusName), nameof(CreatedUtc))]
public sealed class ChatRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CorpusName { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string CitationsJson { get; set; } = string.Empty;
}

[Index(nameof(CorpusName), nameof(CreatedUtc))]
public sealed class PinRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatRecordId { get; set; }

    public string CorpusName { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public string Note { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string CitationsJson { get; set; } = string.Empty;
}

