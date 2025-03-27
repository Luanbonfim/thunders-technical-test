using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Thunders.TechTest.ApiService.Data;
using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Repositories;
using Thunders.TechTest.ApiService.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thunders.TechTest.Tests.Repositories;

public class TollUsageRepositoryTests : IAsyncLifetime
{
    private readonly TollUsageDbContext _dbContext;
    private readonly ITollUsageRepository _repository;
    private readonly Mock<ILogger<TollUsageRepository>> _loggerMock;
    private readonly Mock<ITimeoutService> _timeoutServiceMock;

    public TollUsageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TollUsageDbContext>()
            .UseInMemoryDatabase($"TollUsageDb_{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new TollUsageDbContext(options);
        _loggerMock = new Mock<ILogger<TollUsageRepository>>();
        _timeoutServiceMock = new Mock<ITimeoutService>();

        _timeoutServiceMock
            .Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((string name, Func<CancellationToken, Task<bool>> operation, CancellationToken ct) => operation(ct));

        _timeoutServiceMock
            .Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<Dictionary<string, List<(DateTime Hour, decimal Total)>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((string name, Func<CancellationToken, Task<Dictionary<string, List<(DateTime Hour, decimal Total)>>>> operation, CancellationToken ct) => operation(ct));

        _timeoutServiceMock
            .Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IEnumerable<(string, decimal)>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((string name, Func<CancellationToken, Task<IEnumerable<(string, decimal)>>> operation, CancellationToken ct) => operation(ct));

        _timeoutServiceMock
            .Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<Dictionary<VehicleType, int>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((string name, Func<CancellationToken, Task<Dictionary<VehicleType, int>>> operation, CancellationToken ct) => operation(ct));

        _repository = new TollUsageRepository(_dbContext, _timeoutServiceMock.Object, _loggerMock.Object);
        SeedTestData();
    }

    private void SeedTestData()
    {
        var random = new Random();
        var cities = new[] { "São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba", "Porto Alegre" };
        var tollBooths = new[] { "TB001", "TB002", "TB003", "TB004", "TB005", "TB006", "TB007", "TB008", "TB009", "TB010" };
        var vehicleTypes = Enum.GetValues<VehicleType>();

        var tollUsages = new List<TollUsage>();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        for (var date = startDate; date <= endDate; date = date.AddHours(1))
        {
            foreach (var tollBooth in tollBooths)
            {
                var city = cities[random.Next(cities.Length)];
                var vehicleType = vehicleTypes[random.Next(vehicleTypes.Length)];
                var amount = random.Next(5, 50);

                tollUsages.Add(new TollUsage
                {
                    UsageDateTime = date,
                    TollBooth = tollBooth,
                    City = city,
                    State = "SP", 
                    Amount = amount,
                    VehicleType = vehicleType
                });
            }
        }

        _dbContext.TollUsages.AddRange(tollUsages);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddTollUsages()
    {
        // Arrange
        var tollUsages = new List<TollUsage>
        {
            new() { UsageDateTime = DateTime.Now, TollBooth = "TB001", City = "São Paulo", State = "SP", Amount = 10, VehicleType = VehicleType.Car },
            new() { UsageDateTime = DateTime.Now, TollBooth = "TB002", City = "Rio de Janeiro", State = "RJ", Amount = 15, VehicleType = VehicleType.Truck }
        };

        // Act
        var result = await _repository.CreateAsync(tollUsages, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(_dbContext.TollUsages.Any(x => 
            x.TollBooth == tollUsages[0].TollBooth || x.TollBooth == tollUsages[1].TollBooth));
    }

    [Fact]
    public async Task GetHourlyTotalByCityAsync_ShouldReturnCorrectTotals()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = await _repository.GetHourlyTotalByCityAsync(startDate, endDate, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Verify each city has hourly totals
        foreach (var cityData in result)
        {
            Assert.NotEmpty(cityData.Value);
            
            // Verify each hour has a positive total
            foreach (var hourlyData in cityData.Value)
            {
                Assert.True(hourlyData.Total > 0, $"Total for {cityData.Key} at {hourlyData.Hour} should be positive");
            }
        }
    }

    [Fact]
    public async Task GetTopTollboothsMonthAsync_ShouldReturnCorrectOrder()
    {
        // Arrange
        var count = 5;
        var month = new DateTime(2024, 1, 1);

        // Act
        var result = await _repository.GetTopTollboothsMonthAsync(count, month, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(count, result.Count());
        Assert.Equal(result.OrderByDescending(x => x.TotalAmount), result);
    }

    [Fact]
    public async Task GetVehicleTypesByTollboothAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var tollBooth = "TB001";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = await _repository.GetVehicleTypesByTollboothAsync(tollBooth, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, kvp => Assert.True(kvp.Value > 0));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        return Task.CompletedTask;
    }
} 