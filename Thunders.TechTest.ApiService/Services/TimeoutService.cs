using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Thunders.TechTest.ApiService.Services;

public interface ITimeoutService
{
    Task<T> ExecuteWithTimeoutAsync<T>(string operationName, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

public class TimeoutService : ITimeoutService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TimeoutService> _logger;
    private readonly int _timeoutSeconds;

    public TimeoutService(IConfiguration configuration, ILogger<TimeoutService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _timeoutSeconds = _configuration.GetValue<int>("ApiSettings:TimeoutInSeconds");
    }

    public async Task<T> ExecuteWithTimeoutAsync<T>(string operationName, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

        try
        {
            return await operation(linkedCts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            _logger.LogWarning("Operation {OperationName} timed out after {TimeoutSeconds} seconds", operationName, _timeoutSeconds);
            throw;
        }
    }
} 