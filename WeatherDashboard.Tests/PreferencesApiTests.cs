using System.Text.Json;

namespace WeatherDashboard.Tests;

/// <summary>
/// Integration tests for the Preferences API (Issue #15).
/// Tests CRUD operations against the seeded PostgreSQL database.
/// </summary>
[Collection("AspireApp")]
public class PreferencesApiTests(AspireAppFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetUserPreferences_ReturnsSeededCities()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");

        // Act
        using var response = await httpClient.GetAsync("/api/preferences/demo-user");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preferences = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        Assert.NotNull(preferences);
        Assert.True(preferences.Length >= 3, "demo-user should have at least 3 seeded cities");

        var cities = preferences.Select(p => p.GetProperty("city").GetString()).ToList();
        Assert.Contains("Seattle", cities);
        Assert.Contains("Portland", cities);
        Assert.Contains("Austin", cities);
    }

    [Fact]
    public async Task PostPreference_AddsNewCity()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");
        var uniqueCity = $"TestCity-{Guid.NewGuid():N}"[..20];

        // Act
        using var response = await httpClient.PostAsJsonAsync(
            "/api/preferences/test-user-add", new { City = uniqueCity });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify it was persisted
        using var getResponse = await httpClient.GetAsync("/api/preferences/test-user-add");
        var preferences = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        var cities = preferences!.Select(p => p.GetProperty("city").GetString()).ToList();
        Assert.Contains(uniqueCity, cities);
    }

    [Fact]
    public async Task PostPreference_DuplicateCity_Returns409()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");
        var uniqueCity = $"DupCity-{Guid.NewGuid():N}"[..20];

        // First add
        await httpClient.PostAsJsonAsync("/api/preferences/test-user-dup", new { City = uniqueCity });

        // Act — duplicate add
        using var response = await httpClient.PostAsJsonAsync(
            "/api/preferences/test-user-dup", new { City = uniqueCity });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeletePreference_RemovesCity()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");
        var uniqueCity = $"DelCity-{Guid.NewGuid():N}"[..20];

        await httpClient.PostAsJsonAsync("/api/preferences/test-user-del", new { City = uniqueCity });

        // Act
        using var response = await httpClient.DeleteAsync($"/api/preferences/test-user-del/{uniqueCity}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        using var getResponse = await httpClient.GetAsync("/api/preferences/test-user-del");
        var preferences = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        var cities = preferences!.Select(p => p.GetProperty("city").GetString()).ToList();
        Assert.DoesNotContain(uniqueCity, cities);
    }

    [Fact]
    public async Task GetAllCities_ReturnsDistinctList()
    {
        // Arrange
        using var httpClient = fixture.App.CreateHttpClient("preferencesapi");

        // Act
        using var response = await httpClient.GetAsync("/api/preferences/cities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cities = await response.Content.ReadFromJsonAsync<string[]>(JsonOptions);
        Assert.NotNull(cities);
        Assert.True(cities.Length > 0, "Should return at least one city");

        // Verify distinct
        Assert.Equal(cities.Length, cities.Distinct().Count());

        // Verify sorted
        var sorted = cities.OrderBy(c => c).ToArray();
        Assert.Equal(sorted, cities);
    }
}
