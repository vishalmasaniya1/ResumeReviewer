using ResumeReviewer.Application.DTOs;

namespace ResumeReviewer.Application.Interfaces;

public interface IResumeService
{
    Task<ResumeResponse> UploadResumeAsync(
        string fileName,
        string filePath,
        string candidateEmail,
        string candidateFirstName,
        string candidateLastName,
        string candidatePhone);
        
    Task<IEnumerable<ResumeResponse>> GetAllResumesAsync();
    Task<ResumeResponse?> GetResumeByIdAsync(Guid id);
    Task<bool> DeleteResumeAsync(Guid id);
}
