namespace ResumeReviewer.Application.DTOs;

public record CandidateResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone
);

public record ResumeResponse(
    Guid Id,
    Guid CandidateId,
    CandidateResponse Candidate,
    string FileName,
    List<string> Skills,
    int ExperienceYears,
    string Education, // Serialized JSON or formatted text
    string Projects,  // Serialized JSON or formatted text
    List<string> Certifications,
    DateTime CreatedAt
);
