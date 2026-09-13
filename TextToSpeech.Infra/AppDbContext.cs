using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;

namespace TextToSpeech.Infra;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AudioFile> AudioFiles { get; set; }
    public DbSet<TtsApi> TtsApis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TtsApi>()
            .HasMany(ss => ss.AudioFiles)
            .WithOne(af => af.TtsApi)
            .HasForeignKey(af => af.TtsApiId)
            .OnDelete(DeleteBehavior.SetNull); // Prevent cascading delete

        modelBuilder.Entity<AudioFile>()
            .Property(x => x.OwnerId)
            .HasMaxLength(100);

        modelBuilder.Entity<AudioFile>()
            .HasIndex(x => x.OwnerId);
    }
}