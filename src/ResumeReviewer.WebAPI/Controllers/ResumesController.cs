using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private readonly IResumeService _resumeService;
    private readonly Domain.Interfaces.IResumeRepository _resumeRepository;
    private readonly IWebHostEnvironment _env;

    public ResumesController(
        IResumeService resumeService,
        Domain.Interfaces.IResumeRepository resumeRepository,
        IWebHostEnvironment env)
    {
        _resumeService = resumeService;
        _resumeRepository = resumeRepository;
        _env = env;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string email,
        [FromForm] string firstName,
        [FromForm] string lastName,
        [FromForm] string phone)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx" && extension != ".txt")
        {
            return BadRequest("Unsupported file type. Only PDF, DOCX, and TXT files are allowed.");
        }

        // Create uploads directory if it doesn't exist
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Save file with a unique name to avoid collision
        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var response = await _resumeService.UploadResumeAsync(
            file.FileName,
            filePath,
            email,
            firstName,
            lastName,
            phone
        );

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _resumeService.GetAllResumesAsync();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _resumeService.GetResumeByIdAsync(id);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        var resume = await _resumeRepository.GetByIdAsync(id);
        if (resume == null || !System.IO.File.Exists(resume.FilePath))
        {
            return NotFound("Resume file not found on server.");
        }

        var contentType = "application/octet-stream";
        var extension = Path.GetExtension(resume.FileName).ToLowerInvariant();
        if (extension == ".pdf") contentType = "application/pdf";
        else if (extension == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        else if (extension == ".txt") contentType = "text/plain";

        var fileBytes = await System.IO.File.ReadAllBytesAsync(resume.FilePath);
        return File(fileBytes, contentType, resume.FileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _resumeService.DeleteResumeAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
