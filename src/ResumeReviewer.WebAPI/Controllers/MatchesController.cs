using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;

    public MatchesController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _matchService.GetAllMatchesAsync();
        return Ok(response);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetByJobId(Guid jobId)
    {
        var response = await _matchService.GetMatchesByJobIdAsync(jobId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _matchService.GetMatchDetailsAsync(id);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost("{id}/rerun")]
    public async Task<IActionResult> ReRun(Guid id)
    {
        var success = await _matchService.ReRunMatchAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return Accepted();
    }
}
