namespace ResumeReviewer.Domain.Entities;

public class JobDescription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public int ExperienceYearsMin { get; set; }
    public float[]? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<CandidateJobMatch> Matches { get; set; } = new List<CandidateJobMatch>();
}
