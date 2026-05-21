namespace ResumeReviewer.Application.Interfaces;

public interface IPdfParser
{
    Task<string> ParseAsync(string filePath);
}
