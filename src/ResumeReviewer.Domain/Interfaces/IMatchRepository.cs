using ResumeReviewer.Domain.Entities;

namespace ResumeReviewer.Domain.Interfaces;

public interface IMatchRepository
{
    Task<CandidateJobMatch?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateJobMatch>> GetAllAsync();
    Task<IEnumerable<CandidateJobMatch>> GetByJobIdAsync(Guid jobId);
    Task<IEnumerable<CandidateJobMatch>> GetByResumeIdAsync(Guid resumeId);
    Task<CandidateJobMatch?> GetMatchAsync(Guid jobId, Guid resumeId);
    Task AddAsync(CandidateJobMatch match);
    Task UpdateAsync(CandidateJobMatch match);
    Task DeleteAsync(CandidateJobMatch match);
    Task SaveChangesAsync();
}
