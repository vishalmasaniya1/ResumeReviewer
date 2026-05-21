using Microsoft.EntityFrameworkCore;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Infrastructure.Persistence.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly ApplicationDbContext _context;

    public MatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CandidateJobMatch?> GetByIdAsync(Guid id)
    {
        return await _context.CandidateJobMatches
            .Include(m => m.JobDescription)
            .Include(m => m.Resume)
                .ThenInclude(r => r!.Candidate)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<CandidateJobMatch>> GetAllAsync()
    {
        return await _context.CandidateJobMatches
            .Include(m => m.JobDescription)
            .Include(m => m.Resume)
                .ThenInclude(r => r!.Candidate)
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync();
    }

    public async Task<IEnumerable<CandidateJobMatch>> GetByJobIdAsync(Guid jobId)
    {
        return await _context.CandidateJobMatches
            .Include(m => m.JobDescription)
            .Include(m => m.Resume)
                .ThenInclude(r => r!.Candidate)
            .Where(m => m.JobDescriptionId == jobId)
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync();
    }

    public async Task<IEnumerable<CandidateJobMatch>> GetByResumeIdAsync(Guid resumeId)
    {
        return await _context.CandidateJobMatches
            .Include(m => m.JobDescription)
            .Include(m => m.Resume)
                .ThenInclude(r => r!.Candidate)
            .Where(m => m.ResumeId == resumeId)
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync();
    }

    public async Task<CandidateJobMatch?> GetMatchAsync(Guid jobId, Guid resumeId)
    {
        return await _context.CandidateJobMatches
            .Include(m => m.JobDescription)
            .Include(m => m.Resume)
                .ThenInclude(r => r!.Candidate)
            .FirstOrDefaultAsync(m => m.JobDescriptionId == jobId && m.ResumeId == resumeId);
    }

    public async Task AddAsync(CandidateJobMatch match)
    {
        await _context.CandidateJobMatches.AddAsync(match);
    }

    public async Task UpdateAsync(CandidateJobMatch match)
    {
        _context.Entry(match).State = EntityState.Modified;
        match.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(CandidateJobMatch match)
    {
        _context.CandidateJobMatches.Remove(match);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
