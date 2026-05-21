namespace ResumeReviewer.Application.DTOs;

public class AIAnalysisResult
{
    public double MatchScore { get; set; }
    public List<string> MissingSkills { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> ATSImprovements { get; set; } = new();
    public List<string> InterviewQuestions { get; set; } = new();
    public string FeedbackText { get; set; } = string.Empty;
}
