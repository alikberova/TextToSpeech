using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Entities;
using TextToSpeech.Infra.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AudioFile> AudioFiles { get; set; }
    public DbSet<TtsApi> TtsApis { get; set; }
    internal DbSet<BackgroundJob> BackgroundJobs { get; set; }
    internal DbSet<BackgroundJobAttempt> BackgroundJobAttempts { get; set; }
    internal DbSet<BackgroundJobEvent> BackgroundJobEvents { get; set; }
    internal DbSet<OutboxMessage> OutboxMessages { get; set; }
    internal DbSet<SpeechGenerationRequest> SpeechGenerationRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureJobs();

        var speechRequest = modelBuilder.Entity<SpeechGenerationRequest>();

        speechRequest.ToTable(nameof(SpeechGenerationRequest));

        speechRequest.HasIndex(x => x.JobId)
            .IsUnique();

        speechRequest.HasOne<BackgroundJob>()
            .WithOne()
            .HasForeignKey<SpeechGenerationRequest>(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        speechRequest.Property(x => x.OwnerId)
            .HasMaxLength(100);

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

        modelBuilder.Entity<AudioFile>()
            .HasIndex(x => new { x.Hash, x.OwnerId, x.TtsApiId, x.Status });
    }
}
