using System;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Models.Dtos;

namespace Thunders.TechTest.ApiService.Services;

public interface ITollUsageService
{
    Task<OperationResult<string>> CreateTollUsageAsync(List<TollUsageDto> tollUsages, CancellationToken cancellationToken);
    Task<OperationResult<string>> TriggerReportGenerationAsync(DateTime startDate, DateTime endDate, ReportType reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken);
} 