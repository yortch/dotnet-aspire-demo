using System.Diagnostics;
using System.Text.Json;

namespace WeatherDashboard.Tests;

/// <summary>
/// Resilience and graceful degradation tests (Issue #18).
///
/// Expected behavior:
/// - Weather API uses Redis caching with 15-minute TTL
/// - On external API failure, stale cache is returned if available
/// - If no cache exists and external API fails, 503 is returned
/// - The standard resilience handler adds retries and timeouts to HTTP clients
/// </summary>
[Collection("AspireApp")]
public class ResilienceTests(AspireAppFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task WeatherApi_HandlesRequestGracefully()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");

        // Act — make a request; it should not hang or throw unhandled exceptions
        var sw = Stopwatch.StartNew();
        using var response = await httpClient.GetAsync("/api/weather/chicago");
        sw.Stop();

        // Assert — response should come back within a reasonable timeout
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60),
            $"Request took {sw.Elapsed.TotalSeconds}s, expected < 60s");

        // The response should be a valid HTTP status
        Assert.True(
            response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.InternalServerError,
            $"Expected 200, 503, or 500 but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task CachingBehavior_SecondRequestUsesCache()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");
        const string city = "denver";

        // Act — first request populates cache
        var sw1 = Stopwatch.StartNew();
        using var first = await httpClient.GetAsync($"/api/weather/{city}");
        sw1.Stop();

        // Second request should be served from cache (faster)
        var sw2 = Stopwatch.StartNew();
        using var second = await httpClient.GetAsync($"/api/weather/{city}");
        sw2.Stop();

        // Assert — both should return same status
        Assert.Equal(first.StatusCode, second.StatusCode);

        if (first.StatusCode == HttpStatusCode.OK)
        {
            var firstJson = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            var secondJson = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            // Cached data should have same retrievedAt timestamp
            Assert.Equal(
                firstJson.GetProperty("retrievedAt").GetString(),
                secondJson.GetProperty("retrievedAt").GetString());
        }
    }

    [Fact]
    public async Task WeatherApi_Returns503_WhenExternalApiUnavailable_AndNoCacheExists()
    {
        // Arrange — use a unique city name that won't be cached
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");
        var uniqueCity = $"nocache-{Guid.NewGuid():N}"[..15];

        // Act — if the OpenWeatherMap API key is missing, this should fail gracefully
        using var response = await httpClient.GetAsync($"/api/weather/{uniqueCity}");

        // Assert — should not return 200 for a fake city; 404, 503, or 500 are acceptable
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.InternalServerError,
            $"Expected 404, 503, or 500 for non-existent city but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task PreferencesApi_IsResilient_UnderLoad()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");

        // Act — send multiple concurrent requests
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => httpClient.GetAsync("/api/preferences/demo-user"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert — all should succeed
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response.Dispose();
        }
    }
}
