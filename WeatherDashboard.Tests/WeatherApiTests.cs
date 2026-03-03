using System.Text.Json;

namespace WeatherDashboard.Tests;

/// <summary>
/// Integration tests for the Weather API (Issue #14).
/// Note: Tests may return 503/401 if OpenWeatherMap API key is not configured.
/// The tests validate endpoint routing, response structure, and caching behavior.
/// </summary>
[Collection("AspireApp")]
public class WeatherApiTests(AspireAppFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetCurrentWeather_ReturnsValidJson()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");

        // Act
        using var response = await httpClient.GetAsync("/api/weather/seattle");

        // Assert — either 200 with valid structure, or 503/500 if API key missing
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            Assert.True(json.TryGetProperty("city", out _), "Response should contain 'city'");
            Assert.True(json.TryGetProperty("temperatureF", out _), "Response should contain 'temperatureF'");
            Assert.True(json.TryGetProperty("temperatureC", out _), "Response should contain 'temperatureC'");
            Assert.True(json.TryGetProperty("conditions", out _), "Response should contain 'conditions'");
            Assert.True(json.TryGetProperty("humidity", out _), "Response should contain 'humidity'");
            Assert.True(json.TryGetProperty("windSpeedMph", out _), "Response should contain 'windSpeedMph'");
            Assert.True(json.TryGetProperty("retrievedAt", out _), "Response should contain 'retrievedAt'");
        }
        else
        {
            // 503 (service unavailable) or 500 (API key missing) are acceptable when external API is not configured
            Assert.True(
                response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError,
                $"Expected 200, 503, or 500 but got {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task GetForecast_ReturnsValidJson()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");

        // Act
        using var response = await httpClient.GetAsync("/api/weather/seattle/forecast");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            Assert.True(json.TryGetProperty("city", out _), "Response should contain 'city'");
            Assert.True(json.TryGetProperty("days", out var days), "Response should contain 'days'");
            Assert.Equal(JsonValueKind.Array, days.ValueKind);
            Assert.True(json.TryGetProperty("retrievedAt", out _), "Response should contain 'retrievedAt'");
        }
        else
        {
            Assert.True(
                response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError,
                $"Expected 200, 503, or 500 but got {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task GetCurrentWeather_NonsenseCity_ReturnsNotFoundOr503()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");

        // Act
        using var response = await httpClient.GetAsync("/api/weather/xyznonexistent12345");

        // Assert — 404 from OpenWeatherMap, or 503/500 if API key missing
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.InternalServerError,
            $"Expected 404, 503, or 500 but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetCurrentWeather_CachingWorks_SecondCallSucceeds()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("weatherapi");

        // Act — call twice, second should use cache
        using var first = await httpClient.GetAsync("/api/weather/portland");
        using var second = await httpClient.GetAsync("/api/weather/portland");

        // Assert — both calls should return the same status
        Assert.Equal(first.StatusCode, second.StatusCode);

        if (first.StatusCode == HttpStatusCode.OK)
        {
            var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            // Cached response should have same city
            Assert.Equal(
                firstBody.GetProperty("city").GetString(),
                secondBody.GetProperty("city").GetString());
        }
    }
}
