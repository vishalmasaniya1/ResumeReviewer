namespace ResumeReviewer.Application.DTOs;

public class ParsedResumeData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    public List<string> Skills { get; set; } = new();
    public int ExperienceYears { get; set; }
    public string Education { get; set; } = "[]"; // JSON representation of education history
    public string Projects { get; set; } = "[]";  // JSON representation of projects
    public List<string> Certifications { get; set; } = new();
}
