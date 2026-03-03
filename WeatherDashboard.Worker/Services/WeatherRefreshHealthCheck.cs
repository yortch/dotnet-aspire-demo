using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WeatherDashboard.Worker.Services;

public class WeatherRefreshHealthCheck : IHealthCheck
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(16);

    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;
    private int _lastSuccessCount;
    private int _lastFailureCount;
    private DateTimeOffset? _lastRefreshTime;

    public void RecordResult(int successCount, int failureCount, DateTimeOffset timestamp)
    {
        _lastSuccessCount = successCount;
        _lastFailureCount = failureCount;
        _lastRefreshTime = timestamp;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["lastSuccessCount"] = _lastSuccessCount,
            ["lastFailureCount"] = _lastFailureCount,
            ["lastRefreshTime"] = _lastRefreshTime?.ToString("o") ?? "never"
        };

        if (_lastRefreshTime is null)
        {
            if (DateTimeOffset.UtcNow - _startTime < GracePeriod)
            {
                return Task.FromResult(HealthCheckResult.Healthy("No refresh cycle yet (within grace period)", data: data));
            }
            return Task.FromResult(HealthCheckResult.Unhealthy("No refresh cycle has completed", data: data));
        }

        if (_lastSuccessCount == 0 && _lastFailureCount > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Last refresh entirely failed ({_lastFailureCount} failures)", data: data));
        }

        if (_lastFailureCount > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Partial refresh: {_lastSuccessCount} succeeded, {_lastFailureCount} failed", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Last refresh succeeded for all {_lastSuccessCount} cities", data: data));
    }
}
