using Microsoft.EntityFrameworkCore;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Infrastructure.Persistence.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly ApplicationDbContext _context;

    public ResumeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Resume?> GetByIdAsync(Guid id)
    {
        return await _context.Resumes
            .Include(r => r.Candidate)
            .Include(r => r.Matches)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Resume>> GetAllAsync()
    {
        return await _context.Resumes
            .Include(r => r.Candidate)
            .Include(r => r.Matches)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Resume>> GetByCandidateIdAsync(Guid candidateId)
    {
        return await _context.Resumes
            .Include(r => r.Candidate)
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Resume resume)
    {
        await _context.Resumes.AddAsync(resume);
    }

    public async Task UpdateAsync(Resume resume)
    {
        _context.Entry(resume).State = EntityState.Modified;
        resume.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Resume resume)
    {
        _context.Resumes.Remove(resume);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
