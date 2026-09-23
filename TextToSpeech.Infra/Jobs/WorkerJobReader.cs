using Microsoft.EntityFrameworkCore;
using TextToSpeech.Core.Jobs;
using TextToSpeech.Infra.Jobs.Persistence;

namespace TextToSpeech.Infra.Jobs;

public sealed class WorkerJobReader(AppDbContext context)
{
    public async Task<JobSnapshot?> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await context.BackgroundJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        return job?.Snapshot();
    }

    public async Task<Guid[]> GetExpiredAsync(CancellationToken cancellationToken)
    {
        var now = await context.GetDatabaseTimeAsync(cancellationToken);

        return await context.BackgroundJobs
            .Where(x => x.Status == JobStatus.Running && x.LeaseExpiresAt <= now)
            .OrderBy(x => x.LeaseExpiresAt)
            .Select(x => x.Id)
            .Take(100)
            .ToArrayAsync(cancellationToken);
    }
}
