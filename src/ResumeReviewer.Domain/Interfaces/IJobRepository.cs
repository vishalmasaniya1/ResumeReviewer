using ResumeReviewer.Domain.Entities;

namespace ResumeReviewer.Domain.Interfaces;

public interface IJobRepository
{
    Task<JobDescription?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobDescription>> GetAllAsync();
    Task AddAsync(JobDescription job);
    Task UpdateAsync(JobDescription job);
    Task DeleteAsync(JobDescription job);
    Task SaveChangesAsync();
}
