using Hangfire;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.Infrastructure.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BackgroundJobDispatcher(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void EnqueueResumeProcessing(Guid resumeId)
    {
        _backgroundJobClient.Enqueue<IMatchService>(x => x.ProcessResumeBackgroundAsync(resumeId));
    }

    public void EnqueueJobMatching(Guid jobId, Guid resumeId)
    {
        _backgroundJobClient.Enqueue<IMatchService>(x => x.ProcessJobMatchBackgroundAsync(jobId, resumeId));
    }
}
