using Microsoft.EntityFrameworkCore;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Infrastructure.Persistence.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly ApplicationDbContext _context;

    public CandidateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Candidate?> GetByIdAsync(Guid id)
    {
        return await _context.Candidates
            .Include(c => c.Resumes)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Candidate?> GetByEmailAsync(string email)
    {
        return await _context.Candidates
            .Include(c => c.Resumes)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
    }

    public async Task<IEnumerable<Candidate>> GetAllAsync()
    {
        return await _context.Candidates
            .Include(c => c.Resumes)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Candidate candidate)
    {
        await _context.Candidates.AddAsync(candidate);
    }

    public async Task UpdateAsync(Candidate candidate)
    {
        _context.Entry(candidate).State = EntityState.Modified;
        candidate.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Candidate candidate)
    {
        _context.Candidates.Remove(candidate);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
