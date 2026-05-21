using ResumeReviewer.Application.DTOs;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Enums;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Application.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;

    public JobService(
        IJobRepository jobRepository,
        IResumeRepository resumeRepository,
        IMatchRepository matchRepository,
        IEmbeddingService embeddingService,
        IBackgroundJobDispatcher backgroundJobDispatcher)
    {
        _jobRepository = jobRepository;
        _resumeRepository = resumeRepository;
        _matchRepository = matchRepository;
        _embeddingService = embeddingService;
        _backgroundJobDispatcher = backgroundJobDispatcher;
    }

    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request)
    {
        float[]? embedding = null;
        try
        {
            // Try to pre-calculate embedding for the job description text
            embedding = await _embeddingService.GetEmbeddingAsync(request.Title + "\n" + request.Content);
        }
        catch
        {
            // Log warning or degrade gracefully. We will proceed even if AI is offline
        }

        var job = new JobDescription
        {
            Title = request.Title,
            Department = request.Department,
            Location = request.Location,
            Content = request.Content,
            RequiredSkills = request.RequiredSkills ?? new List<string>(),
            ExperienceYearsMin = request.ExperienceYearsMin,
            Embedding = embedding
        };

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

        // Screen all existing resumes against this new job
        var resumes = await _resumeRepository.GetAllAsync();
        foreach (var resume in resumes)
        {
            var match = new CandidateJobMatch
            {
                JobDescriptionId = job.Id,
                ResumeId = resume.Id,
                Status = MatchStatus.Pending
            };

            await _matchRepository.AddAsync(match);
            await _matchRepository.SaveChangesAsync();

            // Dispatch background matching job
            _backgroundJobDispatcher.EnqueueJobMatching(job.Id, resume.Id);
        }

        return MapToResponse(job);
    }

    public async Task<IEnumerable<JobResponse>> GetAllJobsAsync()
    {
        var jobs = await _jobRepository.GetAllAsync();
        return jobs.Select(MapToResponse);
    }

    public async Task<JobResponse?> GetJobByIdAsync(Guid id)
    {
        var job = await _jobRepository.GetByIdAsync(id);
        if (job == null) return null;

        return MapToResponse(job);
    }

    public async Task<bool> DeleteJobAsync(Guid id)
    {
        var job = await _jobRepository.GetByIdAsync(id);
        if (job == null) return false;

        // Delete associated matches first (handled by EF Core cascade delete, but repositories might need explicit deletes)
        var matches = await _matchRepository.GetByJobIdAsync(id);
        foreach (var match in matches)
        {
            await _matchRepository.DeleteAsync(match);
        }

        await _jobRepository.DeleteAsync(job);
        await _jobRepository.SaveChangesAsync();
        return true;
    }

    private static JobResponse MapToResponse(JobDescription job)
    {
        return new JobResponse(
            job.Id,
            job.Title,
            job.Department,
            job.Location,
            job.Content,
            job.RequiredSkills,
            job.ExperienceYearsMin,
            job.CreatedAt,
            job.Matches?.Count ?? 0
        );
    }
}
