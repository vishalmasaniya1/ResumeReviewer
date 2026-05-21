namespace ResumeReviewer.Application.Interfaces;

public interface IBackgroundJobDispatcher
{
    void EnqueueResumeProcessing(Guid resumeId);
    void EnqueueJobMatching(Guid jobId, Guid resumeId);
}
