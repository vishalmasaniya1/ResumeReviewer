using ResumeReviewer.Domain.Enums;

namespace ResumeReviewer.Application.DTOs;

public record MatchSummaryResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateName,
    Guid ResumeId,
    double MatchScore,
    MatchStatus Status,
    DateTime? ProcessedAt
);

public record MatchDetailsResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateName,
    Guid ResumeId,
    double MatchScore,
    List<string> MissingSkills,
    List<string> Strengths,
    List<string> ATSImprovements,
    List<string> InterviewQuestions,
    string FeedbackText,
    MatchStatus Status,
    DateTime? ProcessedAt,
    DateTime CreatedAt
);
