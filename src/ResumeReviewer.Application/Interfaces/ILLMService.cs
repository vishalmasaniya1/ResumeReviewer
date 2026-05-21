using ResumeReviewer.Application.DTOs;

namespace ResumeReviewer.Application.Interfaces;

public interface ILLMService
{
    Task<ParsedResumeData> ParseResumeStructuredAsync(string resumeText);
    Task<AIAnalysisResult> AnalyzeResumeAlignmentAsync(string resumeText, string jobDescriptionText);
}
