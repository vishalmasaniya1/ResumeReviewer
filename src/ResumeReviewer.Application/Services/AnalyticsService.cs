using ResumeReviewer.Application.DTOs;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Domain.Enums;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IResumeRepository _resumeRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IMatchRepository _matchRepository;

    public AnalyticsService(
        IResumeRepository resumeRepository,
        IJobRepository jobRepository,
        ICandidateRepository candidateRepository,
        IMatchRepository matchRepository)
    {
        _resumeRepository = resumeRepository;
        _jobRepository = jobRepository;
        _candidateRepository = candidateRepository;
        _matchRepository = matchRepository;
    }

    public async Task<AnalyticsSummaryResponse> GetAnalyticsSummaryAsync()
    {
        var resumes = await _resumeRepository.GetAllAsync();
        var jobs = await _jobRepository.GetAllAsync();
        var candidates = await _candidateRepository.GetAllAsync();
        var matches = await _matchRepository.GetAllAsync();

        var completedMatches = matches.Where(m => m.Status == MatchStatus.Completed).ToList();
        
        double avgScore = 0.0;
        if (completedMatches.Any())
        {
            avgScore = Math.Round(completedMatches.Average(m => m.MatchScore), 1);
        }

        var pendingCount = matches.Count(m => m.Status == MatchStatus.Pending || m.Status == MatchStatus.Processing);

        // Map recent matches (take top 5 ordered by creation date/processed date descending)
        var recentMatches = new List<MatchSummaryResponse>();
        var sortedMatches = matches.OrderByDescending(m => m.UpdatedAt).Take(6).ToList();

        foreach (var match in sortedMatches)
        {
            var resume = match.Resume ?? await _resumeRepository.GetByIdAsync(match.ResumeId);
            var candidate = resume?.Candidate ?? await _candidateRepository.GetByIdAsync(resume!.CandidateId);
            var job = match.JobDescription ?? await _jobRepository.GetByIdAsync(match.JobDescriptionId);

            recentMatches.Add(new MatchSummaryResponse(
                match.Id,
                match.JobDescriptionId,
                job?.Title ?? "Unknown Job",
                candidate?.Id ?? Guid.Empty,
                candidate != null ? $"{candidate.FirstName} {candidate.LastName}".Trim() : "Unknown Candidate",
                match.ResumeId,
                match.MatchScore,
                match.Status,
                match.ProcessedAt
            ));
        }

        return new AnalyticsSummaryResponse(
            resumes.Count(),
            jobs.Count(),
            candidates.Count(),
            avgScore,
            pendingCount,
            recentMatches
        );
    }
}
