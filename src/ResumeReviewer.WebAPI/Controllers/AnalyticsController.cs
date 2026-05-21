using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var response = await _analyticsService.GetAnalyticsSummaryAsync();
        return Ok(response);
    }
}
