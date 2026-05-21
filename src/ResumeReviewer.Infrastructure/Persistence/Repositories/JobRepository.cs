using Microsoft.EntityFrameworkCore;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobDescription?> GetByIdAsync(Guid id)
    {
        return await _context.JobDescriptions
            .Include(j => j.Matches)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IEnumerable<JobDescription>> GetAllAsync()
    {
        return await _context.JobDescriptions
            .Include(j => j.Matches)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(JobDescription job)
    {
        await _context.JobDescriptions.AddAsync(job);
    }

    public async Task UpdateAsync(JobDescription job)
    {
        _context.Entry(job).State = EntityState.Modified;
        job.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(JobDescription job)
    {
        _context.JobDescriptions.Remove(job);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
