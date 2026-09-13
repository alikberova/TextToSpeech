using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public static class JobsConfiguration
{
    private const string Schema = "jobs";

    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddScoped<ISubmitBackgroundJob, SubmitBackgroundJob>();
        services.AddScoped<IBackgroundJobs, BackgroundJobs>();

        return services;
    }

    internal static void ConfigureJobs(this ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<BackgroundJob>();

        job.ToTable(nameof(BackgroundJob), Schema);
        job.Property(x => x.OwnerId)
            .HasMaxLength(JobSubmission.MaxOwnerIdLength);
        job.Property(x => x.JobType)
            .HasMaxLength(JobSubmission.MaxJobTypeLength);
        job.Property(x => x.IdempotencyKey)
            .HasMaxLength(JobSubmission.MaxIdempotencyKeyLength);
        job.HasIndex(x => new { x.OwnerId, x.JobType, x.IdempotencyKey })
            .IsUnique();
        job.HasIndex(x => new { x.Status, x.LeaseExpiresAt });

        var attempt = modelBuilder.Entity<BackgroundJobAttempt>();

        attempt.ToTable(nameof(BackgroundJobAttempt), Schema);
        attempt.HasOne<BackgroundJob>()
            .WithMany()
            .HasForeignKey(x => x.JobId);
        attempt.HasIndex(x => new { x.JobId, x.Number })
            .IsUnique();

        var history = modelBuilder.Entity<BackgroundJobEvent>();

        history.ToTable(nameof(BackgroundJobEvent), Schema);
        history.HasOne<BackgroundJob>()
            .WithMany()
            .HasForeignKey(x => x.JobId);

        var outbox = modelBuilder.Entity<OutboxMessage>();

        outbox.ToTable(nameof(OutboxMessage), Schema);
        outbox.HasOne<BackgroundJob>()
            .WithMany()
            .HasForeignKey(x => x.JobId);
        outbox.HasIndex(x => new { x.PublishedAt, x.AvailableAt });
    }
}
