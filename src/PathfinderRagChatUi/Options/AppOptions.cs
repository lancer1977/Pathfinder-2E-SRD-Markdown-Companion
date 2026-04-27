namespace PathfinderRagChatUi.Options;

public sealed class CorpusOptions
{
    public string Name { get; set; } = "pf2e-srd";

    public string RepositoryUrl { get; set; } = "https://github.com/Obsidian-TTRPG-Community/Pathfinder-2E-SRD-Markdown";

    public string Branch { get; set; } = "main";

    public string WorkingDirectory { get; set; } = "data/source";

    public string MarkdownGlob { get; set; } = "**/*.md";

    public bool UseWholeRepo { get; set; } = false;

    public List<string> IncludeRoots { get; set; } = ["compendium", "rules", "fantasy-bestiary"];

    public List<string> ExcludeRoots { get; set; } = [];

    public int ChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 140;
}

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";

    public string EmbeddingModel { get; set; } = "nomic-embed-text:latest";

    public string ChatModel { get; set; } = "qwen2.5-coder:14b";
}

public sealed class DatabaseOptions
{
    public string Path { get; set; } = "data/app.db";
}
