using System.ComponentModel.DataAnnotations;

namespace ResumeReviewer.Application.DTOs;

public record CreateJobRequest(
    [Required] string Title,
    [Required] string Department,
    [Required] string Location,
    [Required] string Content,
    List<string> RequiredSkills,
    int ExperienceYearsMin
);

public record JobResponse(
    Guid Id,
    string Title,
    string Department,
    string Location,
    string Content,
    List<string> RequiredSkills,
    int ExperienceYearsMin,
    DateTime CreatedAt,
    int CandidateMatchesCount
);
