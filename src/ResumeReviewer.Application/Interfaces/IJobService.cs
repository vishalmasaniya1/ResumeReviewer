using ResumeReviewer.Application.DTOs;

namespace ResumeReviewer.Application.Interfaces;

public interface IJobService
{
    Task<JobResponse> CreateJobAsync(CreateJobRequest request);
    Task<IEnumerable<JobResponse>> GetAllJobsAsync();
    Task<JobResponse?> GetJobByIdAsync(Guid id);
    Task<bool> DeleteJobAsync(Guid id);
}
