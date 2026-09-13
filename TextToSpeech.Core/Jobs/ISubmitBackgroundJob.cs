namespace TextToSpeech.Core.Jobs;

public interface ISubmitBackgroundJob
{
    // Joins the caller's transaction when present: domain input + job + outbox must commit together.
    Task<JobAcceptance> SubmitAsync(JobSubmission submission, CancellationToken cancellationToken);
}
