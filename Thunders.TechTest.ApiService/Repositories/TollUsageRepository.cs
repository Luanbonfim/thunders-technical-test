using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Services;

namespace Thunders.TechTest.ApiService.Repositories;

public class TollUsageRepository : ITollUsageRepository
{
    private readonly TollUsageDbContext _dbContext;
    private readonly ITimeoutService _timeoutService;
    private readonly ILogger<TollUsageRepository> _logger;
    private const int BATCH_SIZE = 50000;

    public TollUsageRepository(
        TollUsageDbContext dbContext,
        ITimeoutService timeoutService,
        ILogger<TollUsageRepository> logger)
    {
        _dbContext = dbContext;
        _timeoutService = timeoutService;
        _logger = logger;
    }

    public async Task<bool> CreateAsync(List<TollUsage> tollUsages, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync(
            "CreateTollUsages",
            async (ct) =>
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                try
                {
                    var batches = tollUsages.Chunk(BATCH_SIZE);
                    var tasks = batches.Select(batch => ProcessBatchAsync(batch, ct));
                    await Task.WhenAll(tasks);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating toll usages");
                    throw;
                }
                finally
                {
                    _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
                    _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
                }
            },
            cancellationToken);
    }

    private async Task ProcessBatchAsync(IEnumerable<TollUsage> batch, CancellationToken cancellationToken)
    {
        await _dbContext.TollUsages.AddRangeAsync(batch, cancellationToken);
        await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public async Task<Dictionary<string, List<(DateTime Hour, decimal Total)>>> GetHourlyTotalByCityAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync(
            "GetHourlyTotalByCity",
            async (ct) =>
            {
                _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var results = await _dbContext.TollUsages
                    .Where(t => t.UsageDateTime >= startDate && t.UsageDateTime <= endDate)
                    .GroupBy(t => new { t.City, t.UsageDateTime.Date, t.UsageDateTime.Hour })
                    .Select(g => new
                    {
                        g.Key.City,
                        Hour = new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0),
                        Total = g.Sum(t => t.Amount)
                    })
                    .ToListAsync(ct);

                return results
                    .GroupBy(x => x.City)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => (x.Hour, x.Total)).ToList()
                    );
            },
            cancellationToken);
    }

    public async Task<IEnumerable<(string TollBooth, decimal TotalAmount)>> GetTopTollboothsMonthAsync(int count, DateTime month, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync(
            "GetTopTollboothsMonth",
            async (ct) =>
            {
                _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var startDate = new DateTime(month.Year, month.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var results = await _dbContext.TollUsages
                    .Where(t => t.UsageDateTime >= startDate && t.UsageDateTime <= endDate)
                    .GroupBy(t => t.TollBooth)
                    .Select(g => new
                    {
                        TollBooth = g.Key,
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(count)
                    .ToListAsync(ct);

                return results.Select(x => (x.TollBooth, x.TotalAmount));
            },
            cancellationToken);
    }

    public async Task<Dictionary<VehicleType, int>> GetVehicleTypesByTollboothAsync(string tollBooth, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _timeoutService.ExecuteWithTimeoutAsync(
            "GetVehicleTypesByTollbooth",
            async (ct) =>
            {
                _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var results = await _dbContext.TollUsages
                    .Where(t => t.TollBooth == tollBooth && t.UsageDateTime >= startDate && t.UsageDateTime <= endDate)
                    .GroupBy(t => t.VehicleType)
                    .Select(g => new
                    {
                        VehicleType = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                return results.ToDictionary(x => x.VehicleType, x => x.Count);
            },
            cancellationToken);
    }

    public async Task SaveReportAsync(ReportType reportType, object reportData, DateTime generatedAt, CancellationToken cancellationToken)
    {
        await _timeoutService.ExecuteWithTimeoutAsync(
            "SaveReport",
            async (ct) =>
            {
                _logger.LogInformation("Saving report of type {ReportType} generated at {GeneratedAt}", 
                    reportType, generatedAt);

                // Implementar lógica de persistência do relatório
                // Pode ser em uma tabela separada ou em um sistema de arquivos

                return true;
            },
            cancellationToken);
    }
} 