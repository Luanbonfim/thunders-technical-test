using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Services;
using Thunders.TechTest.ApiService.Middleware;

namespace Thunders.TechTest.ApiService.Repositories;

public class TollUsageRepository : ITollUsageRepository
{
    private readonly TollUsageDbContext _dbContext;
    private readonly ILogger<TollUsageRepository> _logger;
    private readonly ITimeoutService _timeoutService;

    public TollUsageRepository(
        TollUsageDbContext dbContext,
        ILogger<TollUsageRepository> logger,
        ITimeoutService timeoutService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeoutService = timeoutService;
    }

    public async Task<bool> CreateAsync(List<TollUsage> tollUsages, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync("Create", async (ct) =>
        {
            await _dbContext.Set<TollUsage>().AddRangeAsync(tollUsages, ct);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetHourlyTotalByCityAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync("GetHourlyTotalByCity", async (ct) =>
        {
            var results = await _dbContext.TollUsages
                .Where(x => x.UsageDateTime >= startDate && x.UsageDateTime <= endDate)
                .GroupBy(x => new { x.City, Hour = x.UsageDateTime.Hour })
                .Select(g => new
                {
                    g.Key.City,
                    g.Key.Hour,
                    Total = g.Sum(x => x.Amount)
                })
                .ToListAsync(ct);

            return results
                .GroupBy(x => x.City)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Total)
                );
        }, cancellationToken);
    }

    public async Task<IEnumerable<(string TollBooth, decimal TotalAmount)>> GetTopTollboothsMonthAsync(
        int count, 
        DateTime month, 
        CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync("GetTopTollboothsMonth", async (ct) =>
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var results = await _dbContext.Set<TollUsage>()
                .Where(x => x.UsageDateTime >= startOfMonth && x.UsageDateTime < endOfMonth)
                .GroupBy(x => x.TollBooth)
                .Select(g => new
                {
                    TollBooth = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(count)
                .ToListAsync(ct);

            return results.Select(x => (x.TollBooth, x.TotalAmount));
        }, cancellationToken);
    }

    public async Task<Dictionary<VehicleType, int>> GetVehicleTypesByTollboothAsync(
        string tollBooth,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync("GetVehicleTypesByTollbooth", async (ct) =>
        {
            var results = await _dbContext.Set<TollUsage>()
                .Where(x => x.TollBooth == tollBooth && 
                            x.UsageDateTime >= startDate && 
                            x.UsageDateTime <= endDate)
                .GroupBy(x => x.VehicleType)
                .Select(g => new
                {
                    VehicleType = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(ct);

            return results.ToDictionary(x => x.VehicleType, x => x.Count);
        }, cancellationToken);
    }

    public async Task SaveReportAsync(
        ReportType reportType, 
        object reportData, 
        DateTime generatedAt, 
        CancellationToken cancellationToken)
    {
        await _timeoutService.ExecuteWithTimeoutAsync("SaveReport", async (ct) =>
        {
            _logger.LogInformation("Saving report of type {ReportType} generated at {GeneratedAt}", 
                reportType, generatedAt);
            // Implementar lógica de persistência do relatório
            // Pode ser em uma tabela separada ou em um sistema de arquivos
            return true;
        }, cancellationToken);
    }
} 