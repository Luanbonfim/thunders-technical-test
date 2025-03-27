using Microsoft.AspNetCore.Mvc;
using Thunders.TechTest.ApiService.Services;
using Asp.Versioning;
using Thunders.TechTest.ApiService.Models.Dtos;
using Microsoft.AspNetCore.Http.Timeouts;

namespace Thunders.TechTest.ApiService.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class TollUsageController : ControllerBase
{
    private readonly ITollUsageService _tollUsageService;

    public TollUsageController(ITollUsageService tollUsageService)
    {
        _tollUsageService = tollUsageService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTollUsages(
        [FromBody] List<TollUsageDto> tollUsages,
        CancellationToken cancellationToken)
    {
        try 
        {
            var result = await _tollUsageService.CreateTollUsageAsync(tollUsages, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        catch (OperationCanceledException)
        {
            return StatusCode(408, "Request timeout");
        }
    }

    [HttpPost("generate-report")]
    public async Task<IActionResult> GenerateReport(
        [FromBody] ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tollUsageService.TriggerReportGenerationAsync(
                request.StartDate, 
                request.EndDate, 
                request.ReportType, 
                request.Parameters, 
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, "Request timeout");
        }
    }
} 