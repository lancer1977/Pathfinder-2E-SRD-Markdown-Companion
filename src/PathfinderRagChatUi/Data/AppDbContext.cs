using Microsoft.EntityFrameworkCore;

namespace PathfinderRagChatUi.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CorpusSnapshot> CorpusSnapshots => Set<CorpusSnapshot>();

    public DbSet<CorpusChunk> CorpusChunks => Set<CorpusChunk>();

    public DbSet<ChatRecord> ChatRecords => Set<ChatRecord>();

    public DbSet<PinRecord> PinRecords => Set<PinRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CorpusSnapshot>(entity =>
        {
            entity.Property(x => x.CorpusName).HasMaxLength(128);
            entity.Property(x => x.RepositoryUrl).HasMaxLength(512);
            entity.Property(x => x.Branch).HasMaxLength(128);
            entity.Property(x => x.CommitSha).HasMaxLength(128);
            entity.Property(x => x.CheckoutRoot).HasMaxLength(512);
            entity.Property(x => x.EmbeddingModel).HasMaxLength(128);
            entity.Property(x => x.ChatModel).HasMaxLength(128);
        });

        modelBuilder.Entity<CorpusChunk>(entity =>
        {
            entity.Property(x => x.CorpusName).HasMaxLength(128);
            entity.Property(x => x.SourcePath).HasMaxLength(512);
            entity.Property(x => x.SourceTitle).HasMaxLength(256);
            entity.Property(x => x.Text).HasColumnType("TEXT");
            entity.Property(x => x.EmbeddingJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<ChatRecord>(entity =>
        {
            entity.Property(x => x.CorpusName).HasMaxLength(128);
            entity.Property(x => x.Topic).HasMaxLength(256);
            entity.Property(x => x.Question).HasColumnType("TEXT");
            entity.Property(x => x.Answer).HasColumnType("TEXT");
            entity.Property(x => x.Model).HasMaxLength(128);
            entity.Property(x => x.CitationsJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<PinRecord>(entity =>
        {
            entity.Property(x => x.CorpusName).HasMaxLength(128);
            entity.Property(x => x.Note).HasColumnType("TEXT");
            entity.Property(x => x.Topic).HasMaxLength(256);
            entity.Property(x => x.Question).HasColumnType("TEXT");
            entity.Property(x => x.Answer).HasColumnType("TEXT");
            entity.Property(x => x.CitationsJson).HasColumnType("TEXT");
        });
    }
}

