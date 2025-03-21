using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Repositories;
using Thunders.TechTest.ApiService.Services;

namespace Thunders.TechTest.Tests.Performance;

public class TollUsagePerformanceTests : IAsyncLifetime
{
    private readonly TollUsageDbContext _dbContext;
    private readonly ITollUsageRepository _repository;
    private const int MILLION = 1_000_000;
    private const int TEN_MILLION = 10_000_000;

    public TollUsagePerformanceTests()
    {
        var options = new DbContextOptionsBuilder<TollUsageDbContext>()
            .UseInMemoryDatabase($"TollUsageDb_Perf_{Guid.NewGuid()}")
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ApiSettings:TimeoutInSeconds", "10"}
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDbContext<TollUsageDbContext>(options => options.UseInMemoryDatabase($"TollUsageDb_Perf_{Guid.NewGuid()}"));
        services.AddScoped<ITollUsageRepository, TollUsageRepository>();
        services.AddScoped<ITimeoutService, TimeoutService>();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddSingleton<IConfiguration>(configuration);

        var serviceProvider = services.BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<TollUsageDbContext>();
        _repository = serviceProvider.GetRequiredService<ITollUsageRepository>();
    }

    [Fact]
    public async Task CreateAsync_ShouldHandleMillionRecords()
    {
        // Arrange
        var tollUsages = GenerateTollUsages(MILLION);
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _repository.CreateAsync(tollUsages, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(MILLION, _dbContext.TollUsages.Count());
        
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(duration < 10, $"Operation took {duration} seconds, should be under 10 seconds");
    }

    [Fact]
    public async Task GetHourlyTotalByCityAsync_ShouldHandleMillionRecords()
    {
        // Arrange
        await SeedLargeDataset(MILLION);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _repository.GetHourlyTotalByCityAsync(startDate, endDate, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(duration < 10, $"Operation took {duration} seconds, should be under 10 seconds");
    }

    [Fact]
    public async Task GetTopTollboothsMonthAsync_ShouldHandleMillionRecords()
    {
        // Arrange
        await SeedLargeDataset(MILLION);
        var count = 5;
        var month = new DateTime(2024, 1, 1);
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _repository.GetTopTollboothsMonthAsync(count, month, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(count, result.Count());
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(duration < 10, $"Operation took {duration} seconds, should be under 10 seconds");
    }

    [Fact]
    public async Task GetVehicleTypesByTollboothAsync_ShouldHandleMillionRecords()
    {
        // Arrange
        await SeedLargeDataset(MILLION);
        var tollBooth = "TB001";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _repository.GetVehicleTypesByTollboothAsync(tollBooth, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(duration < 10, $"Operation took {duration} seconds, should be under 10 seconds");
    }

    private async Task SeedLargeDataset(int count)
    {
        var tollUsages = GenerateTollUsages(count);
        await _dbContext.TollUsages.AddRangeAsync(tollUsages);
        await _dbContext.SaveChangesAsync();
    }

    private List<TollUsage> GenerateTollUsages(int count)
    {
        var random = new Random();
        var cities = new[] { "São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba", "Porto Alegre" };
        var tollBooths = new[] { "TB001", "TB002", "TB003", "TB004", "TB005", "TB006", "TB007", "TB008", "TB009", "TB010" };
        var vehicleTypes = Enum.GetValues<VehicleType>();

        var tollUsages = new List<TollUsage>();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        for (int i = 0; i < count; i++)
        {
            var date = startDate.AddHours(i % (int)(endDate - startDate).TotalHours);
            var tollBooth = tollBooths[random.Next(tollBooths.Length)];
            var city = cities[random.Next(cities.Length)];
            var vehicleType = vehicleTypes[random.Next(vehicleTypes.Length)];
            var amount = random.Next(5, 50);

            tollUsages.Add(new TollUsage
            {
                Id = Guid.NewGuid(),
                UsageDateTime = date,
                TollBooth = tollBooth,
                City = city,
                State = "SP",
                Amount = amount,
                VehicleType = vehicleType
            });
        }

        return tollUsages;
    }

    public async Task InitializeAsync()
    {
        // Any initialization if needed
    }

    public async Task DisposeAsync()
    {
        _dbContext.Database.EnsureDeleted();
        await _dbContext.DisposeAsync();
    }
} 