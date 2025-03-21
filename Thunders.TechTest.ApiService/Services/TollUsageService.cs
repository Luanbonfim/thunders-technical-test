using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Models.Dtos;
using Thunders.TechTest.OutOfBox.Queues;

namespace Thunders.TechTest.ApiService.Services;

public class TollUsageService : ITollUsageService
{
    private readonly IMessageSender _messageSender;
    private readonly ILogger<TollUsageService> _logger;

    public TollUsageService(
        IMessageSender messageSender,
        ILogger<TollUsageService> logger)
    {
        _messageSender = messageSender;
        _logger = logger;
    }

    public async Task<OperationResult<string>> CreateTollUsageAsync(List<TollUsageDto> tollUsages, CancellationToken cancellationToken)
    {
        try
        {
            if (tollUsages == null || !tollUsages.Any())
            {
                return OperationResult<string>.Failure("No toll usages provided");
            }

            var currentDate = DateTime.UtcNow;

            foreach (var usage in tollUsages)
            {
                var validateDates = ValidateUsageDates(currentDate, usage.UsageDateTime);

                if (!validateDates.IsSuccess)
                {
                    return validateDates;
                }
                
                if (usage.Amount <= 0)
                {
                    return OperationResult<string>.Failure("Amount must be greater than 0");
                }
            }

            var message = new TollUsageMessage
            {
                Id = Guid.NewGuid(),
                TollUsages = tollUsages
            }; 

            await _messageSender.SendLocal(message);
            return OperationResult<string>.Success("Toll Usages Creation Successfully Triggered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating toll usage");
            return OperationResult<string>.Failure("Error creating toll usage");
        }
    }

    public async Task<OperationResult<string>> TriggerReportGenerationAsync(DateTime startDate, DateTime endDate, ReportType reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        try
        {
            if (!Enum.IsDefined(typeof(ReportType), reportType))
            {
                return OperationResult<string>.Failure("Invalid report type");
            }

            var validateDates = ValidadeReportDates(startDate, endDate);

            if (!validateDates.IsSuccess)
            {
                return validateDates;
            }

            var message = new ReportGenerationMessage
            {
                GeneratedAt = DateTime.UtcNow,
                ReportType = reportType,
                Parameters = parameters,
                StartDate = startDate,
                EndDate = endDate
            };

            await _messageSender.SendLocal(message);
            return OperationResult<string>.Success("Report Generation Successfully Triggered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering report generation");
            return OperationResult<string>.Failure("Error triggering report generation");
        }
    }

    private OperationResult<string> ValidateUsageDates(DateTime currentDate, DateTime usageDate)
    {
        if (usageDate == DateTime.MinValue)
        {
            return OperationResult<string>.Failure("UsageDateTime cannot be empty");
        }

        if (usageDate > currentDate)
        {
            return OperationResult<string>.Failure("UsageDateTime cannot be in the future");
        }

        return OperationResult<string>.Success("Dates are valid");
    }

    private OperationResult<string> ValidadeReportDates(DateTime startDate, DateTime endDate)
    {
        if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
        {
            return OperationResult<string>.Failure("StartDate and EndDate are required");
        }

        var currentDate = DateTime.UtcNow;

        if (startDate > currentDate)
        {
            return OperationResult<string>.Failure("StartDate cannot be in the future");
        }

        if (startDate > endDate)
        {
            return OperationResult<string>.Failure("StartDate cannot be greater than endDate");
        }

        return OperationResult<string>.Success("Dates are valid");
    }
} 
