using ResumeReviewer.Domain.Entities;

namespace ResumeReviewer.Domain.Interfaces;

public interface IResumeRepository
{
    Task<Resume?> GetByIdAsync(Guid id);
    Task<IEnumerable<Resume>> GetAllAsync();
    Task<IEnumerable<Resume>> GetByCandidateIdAsync(Guid candidateId);
    Task AddAsync(Resume resume);
    Task UpdateAsync(Resume resume);
    Task DeleteAsync(Resume resume);
    Task SaveChangesAsync();
}
