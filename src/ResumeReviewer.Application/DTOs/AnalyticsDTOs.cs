namespace ResumeReviewer.Application.DTOs;

public record AnalyticsSummaryResponse(
    int TotalResumes,
    int TotalJobs,
    int TotalCandidates,
    double AverageMatchScore,
    int PendingMatchesCount,
    List<MatchSummaryResponse> RecentMatches
);
