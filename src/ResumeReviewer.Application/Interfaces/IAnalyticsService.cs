using ResumeReviewer.Application.DTOs;

namespace ResumeReviewer.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryResponse> GetAnalyticsSummaryAsync();
}
