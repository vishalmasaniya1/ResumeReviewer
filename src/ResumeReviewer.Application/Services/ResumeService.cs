using ResumeReviewer.Application.DTOs;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Interfaces;

namespace ResumeReviewer.Application.Services;

public class ResumeService : IResumeService
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;

    public ResumeService(
        ICandidateRepository candidateRepository,
        IResumeRepository resumeRepository,
        IBackgroundJobDispatcher backgroundJobDispatcher)
    {
        _candidateRepository = candidateRepository;
        _resumeRepository = resumeRepository;
        _backgroundJobDispatcher = backgroundJobDispatcher;
    }

    public async Task<ResumeResponse> UploadResumeAsync(
        string fileName,
        string filePath,
        string candidateEmail,
        string candidateFirstName,
        string candidateLastName,
        string candidatePhone)
    {
        // 1. Get or create candidate
        var candidate = await _candidateRepository.GetByEmailAsync(candidateEmail);
        if (candidate == null)
        {
            candidate = new Candidate
            {
                Email = candidateEmail,
                FirstName = candidateFirstName,
                LastName = candidateLastName,
                Phone = candidatePhone
            };
            await _candidateRepository.AddAsync(candidate);
            await _candidateRepository.SaveChangesAsync();
        }
        else
        {
            // Update candidate details if changed
            if (!string.IsNullOrEmpty(candidateFirstName)) candidate.FirstName = candidateFirstName;
            if (!string.IsNullOrEmpty(candidateLastName)) candidate.LastName = candidateLastName;
            if (!string.IsNullOrEmpty(candidatePhone)) candidate.Phone = candidatePhone;
            await _candidateRepository.UpdateAsync(candidate);
            await _candidateRepository.SaveChangesAsync();
        }

        // 2. Create resume record
        var resume = new Resume
        {
            CandidateId = candidate.Id,
            FileName = fileName,
            FilePath = filePath,
            ParsedText = "Processing..." // Will be updated by background job
        };

        await _resumeRepository.AddAsync(resume);
        await _resumeRepository.SaveChangesAsync();

        // 3. Dispatch background job to extract text, parse using AI, generate embeddings, and schedule job matches
        _backgroundJobDispatcher.EnqueueResumeProcessing(resume.Id);

        return MapToResponse(resume, candidate);
    }

    public async Task<IEnumerable<ResumeResponse>> GetAllResumesAsync()
    {
        var resumes = await _resumeRepository.GetAllAsync();
        var responses = new List<ResumeResponse>();

        foreach (var resume in resumes)
        {
            var candidate = resume.Candidate ?? await _candidateRepository.GetByIdAsync(resume.CandidateId);
            responses.Add(MapToResponse(resume, candidate!));
        }

        return responses;
    }

    public async Task<ResumeResponse?> GetResumeByIdAsync(Guid id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        if (resume == null) return null;

        var candidate = resume.Candidate ?? await _candidateRepository.GetByIdAsync(resume.CandidateId);
        return MapToResponse(resume, candidate!);
    }

    public async Task<bool> DeleteResumeAsync(Guid id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        if (resume == null) return false;

        // Try to delete physical file
        try
        {
            if (File.Exists(resume.FilePath))
            {
                File.Delete(resume.FilePath);
            }
        }
        catch
        {
            // Ignore file deletion errors to prevent DB state mismatch
        }

        await _resumeRepository.DeleteAsync(resume);
        await _resumeRepository.SaveChangesAsync();
        return true;
    }

    private static ResumeResponse MapToResponse(Resume resume, Candidate candidate)
    {
        var candidateDto = new CandidateResponse(
            candidate.Id,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email,
            candidate.Phone
        );

        return new ResumeResponse(
            resume.Id,
            resume.CandidateId,
            candidateDto,
            resume.FileName,
            resume.Skills ?? new List<string>(),
            resume.ExperienceYears,
            resume.Education,
            resume.Projects,
            resume.Certifications ?? new List<string>(),
            resume.CreatedAt
        );
    }
}
