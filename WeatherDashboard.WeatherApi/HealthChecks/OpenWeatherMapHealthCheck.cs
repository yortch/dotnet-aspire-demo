using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WeatherDashboard.WeatherApi.HealthChecks;

public class OpenWeatherMapHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = configuration["OpenWeatherMap:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Unhealthy("OpenWeatherMap API key is not configured.");
            }

            var client = httpClientFactory.CreateClient(nameof(Services.OpenWeatherMapService));
            // Lightweight call using a well-known city
            var response = await client.GetAsync(
                $"data/2.5/weather?q=London&appid={apiKey}&units=imperial",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("OpenWeatherMap API is reachable.")
                : HealthCheckResult.Degraded($"OpenWeatherMap API returned {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OpenWeatherMap API is unreachable.", ex);
        }
    }
}
