using Microsoft.AspNetCore.Mvc;
using Thunders.TechTest.ApiService.Services;
using Thunders.TechTest.ApiService.Middleware;
using Asp.Versioning;
using Thunders.TechTest.ApiService.Models.Dtos;

namespace Thunders.TechTest.ApiService.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TollUsageController : ControllerBase
{
    private readonly ITollUsageService _tollUsageService;
    private readonly ILogger<TollUsageController> _logger;
    private readonly ITimeoutService _timeoutService;

    public TollUsageController(
        ITollUsageService tollUsageService,
        ILogger<TollUsageController> logger,
        ITimeoutService timeoutService)
    {
        _tollUsageService = tollUsageService;
        _logger = logger;
        _timeoutService = timeoutService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTollUsages(
        [FromBody] List<TollUsageDto> tollUsages,
        CancellationToken cancellationToken)
    {
        var result = await _timeoutService.ExecuteWithTimeoutAsync("CreateTollUsages", async (ct) =>
        {
            return await _tollUsageService.CreateTollUsageAsync(tollUsages, ct);
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("generate-report")]
    public async Task<IActionResult> GenerateReport(
        [FromBody] ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _timeoutService.ExecuteWithTimeoutAsync("GenerateReport", async (ct) =>
        {
            return await _tollUsageService.TriggerReportGenerationAsync(request.StartDate, request.EndDate, request.ReportType, request.Parameters, ct);
        }, cancellationToken);

        return Ok(result);
    }
} 