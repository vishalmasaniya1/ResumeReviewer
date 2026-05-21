using ResumeReviewer.Domain.Entities;

namespace ResumeReviewer.Domain.Interfaces;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id);
    Task<Candidate?> GetByEmailAsync(string email);
    Task<IEnumerable<Candidate>> GetAllAsync();
    Task AddAsync(Candidate candidate);
    Task UpdateAsync(Candidate candidate);
    Task DeleteAsync(Candidate candidate);
    Task SaveChangesAsync();
}
