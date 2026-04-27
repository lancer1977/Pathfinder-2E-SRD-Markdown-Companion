namespace PathfinderRagChatUi.Contracts;

public sealed record RefreshProgressDto(
    string Stage,
    string Message,
    int ProcessedFiles,
    int TotalFiles,
    int ProcessedChunks,
    int TotalChunks);

public sealed record CorpusRefreshRequestDto(
    bool UseWholeRepo = false,
    IReadOnlyList<string>? IncludeRoots = null,
    IReadOnlyList<string>? ExcludeRoots = null);

public sealed record RefreshResultDto(
    string CorpusName,
    string RepositoryUrl,
    string Branch,
    string? CommitSha,
    string CheckoutRoot,
    int FileCount,
    int ChunkCount,
    DateTimeOffset UpdatedUtc);

public sealed record CorpusSummaryDto(
    string CorpusName,
    string RepositoryUrl,
    string Branch,
    string? CommitSha,
    string CheckoutRoot,
    int FileCount,
    int ChunkCount,
    DateTimeOffset UpdatedUtc,
    string EmbeddingModel,
    string ChatModel);

public sealed record CitationDto(
    int Rank,
    double Score,
    Guid ChunkId,
    int ChunkIndex,
    string SourcePath,
    string SourceTitle,
    string Preview);

public sealed record SourceChunkDto(
    string CorpusName,
    Guid ChunkId,
    string SourcePath,
    string SourceTitle,
    int ChunkIndex,
    string Text);

public sealed record ChatRequestDto(
    string Question,
    int TopK = 8,
    string? Model = null,
    string? CorpusName = null);

public sealed record ChatPreparationDto(
    string CorpusName,
    string SystemPrompt,
    string Prompt,
    IReadOnlyList<CitationDto> Citations);

public sealed record ChatResponseDto(
    Guid RecordId,
    string CorpusName,
    string Question,
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    string Model,
    string Topic);

public sealed record HistoryItemDto(
    Guid RecordId,
    string CorpusName,
    DateTimeOffset CreatedUtc,
    string Topic,
    string Question,
    string Answer,
    string Model,
    IReadOnlyList<CitationDto> Citations);

public sealed record PinRequestDto(
    Guid RecordId,
    string? Note);

public sealed record PinDto(
    Guid PinId,
    Guid RecordId,
    string CorpusName,
    DateTimeOffset CreatedUtc,
    string Topic,
    string Question,
    string Answer,
    string Note,
    IReadOnlyList<CitationDto> Citations);
