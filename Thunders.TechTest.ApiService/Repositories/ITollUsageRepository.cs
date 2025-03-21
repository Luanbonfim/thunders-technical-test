using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Messages;
using Thunders.TechTest.ApiService.Models;

namespace Thunders.TechTest.ApiService.Repositories;

public interface ITollUsageRepository
{
    Task<bool> CreateAsync(List<TollUsage> tollUsages, CancellationToken cancellationToken);
    Task<Dictionary<string, List<(DateTime Hour, decimal Total)>>> GetHourlyTotalByCityAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken);
    Task<IEnumerable<(string TollBooth, decimal TotalAmount)>> GetTopTollboothsMonthAsync(
        int count, 
        DateTime month, 
        CancellationToken cancellationToken);
    Task<Dictionary<VehicleType, int>> GetVehicleTypesByTollboothAsync(
        string tollBooth,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
    Task SaveReportAsync(
        ReportType reportType, 
        object reportData, 
        DateTime generatedAt, 
        CancellationToken cancellationToken);
} 