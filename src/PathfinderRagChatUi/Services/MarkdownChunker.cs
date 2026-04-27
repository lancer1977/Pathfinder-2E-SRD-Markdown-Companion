namespace PathfinderRagChatUi.Services;

public sealed class MarkdownChunker
{
    public IReadOnlyList<string> Chunk(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Trim();
        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var end = Math.Min(start + chunkSize, normalized.Length);
            var chunk = normalized[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (end >= normalized.Length)
            {
                break;
            }

            var nextStart = Math.Max(0, end - overlap);
            start = Math.Max(start + 1, nextStart);
        }

        return chunks;
    }
}
