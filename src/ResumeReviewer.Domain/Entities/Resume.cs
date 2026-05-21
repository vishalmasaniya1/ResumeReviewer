namespace ResumeReviewer.Domain.Entities;

public class Resume
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public virtual Candidate? Candidate { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ParsedText { get; set; } = string.Empty;
    
    // Structured data
    public List<string> Skills { get; set; } = new();
    public int ExperienceYears { get; set; }
    public string Education { get; set; } = string.Empty; // Serialized JSON array of structures
    public string Projects { get; set; } = string.Empty;  // Serialized JSON array of structures
    public List<string> Certifications { get; set; } = new();

    public float[]? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<CandidateJobMatch> Matches { get; set; } = new List<CandidateJobMatch>();
}
