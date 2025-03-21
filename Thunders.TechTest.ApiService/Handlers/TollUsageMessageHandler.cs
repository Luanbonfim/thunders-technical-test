using Rebus.Handlers;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Repositories;
using Thunders.TechTest.ApiService.Services;
using Thunders.TechTest.ApiService.Middleware;

namespace Thunders.TechTest.ApiService.Handlers;

public class TollUsageMessageHandler : IHandleMessages<TollUsageMessage>, IHandleMessages<ReportGenerationMessage>
{
    private readonly ITollUsageRepository _repository;
    private readonly ILogger<TollUsageMessageHandler> _logger;
    private readonly ITimeoutService _timeoutService;

    public TollUsageMessageHandler(
        ITollUsageRepository repository,
        ILogger<TollUsageMessageHandler> logger,
        ITimeoutService timeoutService)
    {
        _repository = repository;
        _logger = logger;
        _timeoutService = timeoutService;
    }

    public async Task Handle(TollUsageMessage message)
    {
        await _timeoutService.ExecuteWithTimeoutAsync("HandleTollUsageMessage", async (ct) =>
        {
            var tollUsages = message.TollUsages.Select(dto => new TollUsage
            {
                Id = dto.Id ?? Guid.NewGuid(),
                UsageDateTime = dto.UsageDateTime,
                TollBooth = dto.TollBooth,
                City = dto.City,
                State = dto.State,
                Amount = dto.Amount,
                VehicleType = dto.VehicleType
            }).ToList();

            await _repository.CreateAsync(tollUsages, ct);
            _logger.LogInformation("Processed {Count} toll usages from message ID: {Id}", tollUsages.Count, message.Id);
            return true;
        }, CancellationToken.None);
    }

    public async Task Handle(ReportGenerationMessage message)
    {
        await _timeoutService.ExecuteWithTimeoutAsync("HandleReportGenerationMessage", async (ct) =>
        {
            switch (message.ReportType)
            {
                case ReportType.HourlyByCityReport:
                    await GenerateHourlyByCityReport(message.StartDate, message.EndDate, message.GeneratedAt, ct);
                    break;

                case ReportType.TopTollboothsReport:
                    if (!message.Parameters.TryGetValue("tollboothsAmount", out var tollboothsAmountParameter))
                        throw new ArgumentException("tollboothsAmount parameter is required for top tollbooths report");

                    if (!int.TryParse(tollboothsAmountParameter?.ToString(), out int tollboothsAmount))
                    {
                        throw new ArgumentException("tollboothsAmount parameter must be a valid number");
                    }

                    await GenerateTopTollboothsReport(
                        tollboothsAmount, 
                        message.StartDate, 
                        message.GeneratedAt,
                        ct);
                    break;

                case ReportType.VehicleTypesByTollboothReport:
                    if (!message.Parameters.TryGetValue("tollBoothId", out var tollBoothIdParameter))
                        throw new ArgumentException("tollBoothId parameter is required for vehicle types report");

                    if (!Guid.TryParse(tollBoothIdParameter?.ToString(), out Guid _))
                    {
                        throw new ArgumentException("tollBoothId parameter must be a valid GUID");
                    }

                    await GenerateVehicleTypesByTollboothReport(
                        tollBoothIdParameter.ToString()!,
                        message.StartDate, 
                        message.EndDate,
                        message.GeneratedAt,
                        ct);
                    break;
            }
            
            _logger.LogInformation("Generated report(s) at: {GeneratedAt}", message.GeneratedAt);
            return true;
        }, CancellationToken.None);
    }

    private async Task GenerateHourlyByCityReport(DateTime startDate, DateTime endDate, DateTime generatedAt, CancellationToken cancellationToken)
    {
        var hourlyByCity = await _repository.GetHourlyTotalByCityAsync(startDate, endDate, cancellationToken);
        await _repository.SaveReportAsync(ReportType.HourlyByCityReport, hourlyByCity, generatedAt, cancellationToken);
        _logger.LogInformation("Generated hourly by city report at: {GeneratedAt}", generatedAt);
    }

    private async Task GenerateTopTollboothsReport(int tollboothsAmount, DateTime startDate, DateTime generatedAt, CancellationToken cancellationToken)
    {
        var topTollbooths = await _repository.GetTopTollboothsMonthAsync(tollboothsAmount, startDate, cancellationToken);
        await _repository.SaveReportAsync(ReportType.TopTollboothsReport, topTollbooths, generatedAt, cancellationToken);
        _logger.LogInformation("Generated top {TollboothsAmount} tollbooths report at: {GeneratedAt}", tollboothsAmount, generatedAt);
    }

    private async Task GenerateVehicleTypesByTollboothReport(string tollBoothId, DateTime startDate, DateTime endDate, DateTime generatedAt, CancellationToken cancellationToken)
    {
        var vehicleTypes = await _repository.GetVehicleTypesByTollboothAsync(tollBoothId, startDate, endDate, cancellationToken);
        await _repository.SaveReportAsync(ReportType.VehicleTypesByTollboothReport, vehicleTypes, generatedAt, cancellationToken);
        _logger.LogInformation("Generated vehicle types report for tollbooth {TollBoothId} at: {GeneratedAt}", tollBoothId, generatedAt);
    }
} 