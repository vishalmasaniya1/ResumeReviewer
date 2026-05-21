using ResumeReviewer.Application.DTOs;

namespace ResumeReviewer.Application.Interfaces;

public interface IMatchService
{
    Task<IEnumerable<MatchSummaryResponse>> GetMatchesByJobIdAsync(Guid jobId);
    Task<MatchDetailsResponse?> GetMatchDetailsAsync(Guid matchId);
    Task<bool> ReRunMatchAsync(Guid matchId);
    Task<IEnumerable<MatchSummaryResponse>> GetAllMatchesAsync();
    
    // Background execution methods (Hangfire entries)
    Task ProcessResumeBackgroundAsync(Guid resumeId);
    Task ProcessJobMatchBackgroundAsync(Guid jobId, Guid resumeId);
}
