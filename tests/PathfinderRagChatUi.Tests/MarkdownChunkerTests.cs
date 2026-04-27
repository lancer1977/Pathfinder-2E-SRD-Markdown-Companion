using PathfinderRagChatUi.Services;

namespace PathfinderRagChatUi.Tests;

public sealed class MarkdownChunkerTests
{
    [Fact]
    public void Chunk_SplitsLargeText_WithOverlap()
    {
        var chunker = new MarkdownChunker();
        var text = string.Concat(Enumerable.Range(0, 200).Select(i => $"Line {i}\n"));

        var chunks = chunker.Chunk(text, chunkSize: 120, overlap: 20);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk)));
    }
}

