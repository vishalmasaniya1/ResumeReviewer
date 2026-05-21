using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeReviewer.Application.DTOs;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IValidator<CreateJobRequest> _validator;

    public JobsController(IJobService jobService, IValidator<CreateJobRequest> validator)
    {
        _jobService = jobService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var response = await _jobService.CreateJobAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _jobService.GetAllJobsAsync();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _jobService.GetJobByIdAsync(id);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _jobService.DeleteJobAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
