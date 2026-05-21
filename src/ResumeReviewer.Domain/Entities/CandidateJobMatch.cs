using ResumeReviewer.Domain.Enums;

namespace ResumeReviewer.Domain.Entities;

public class CandidateJobMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ResumeId { get; set; }
    public virtual Resume? Resume { get; set; }

    public Guid JobDescriptionId { get; set; }
    public virtual JobDescription? JobDescription { get; set; }

    public double MatchScore { get; set; } // Cosine similarity combined with LLM score
    public List<string> MissingSkills { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> ATSImprovements { get; set; } = new();
    public List<string> InterviewQuestions { get; set; } = new();
    public string FeedbackText { get; set; } = string.Empty;

    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
