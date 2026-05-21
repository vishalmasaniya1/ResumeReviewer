namespace ResumeReviewer.Application.Interfaces;

public interface IDocxParser
{
    Task<string> ParseAsync(string filePath);
}
