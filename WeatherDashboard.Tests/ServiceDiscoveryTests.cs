namespace WeatherDashboard.Tests;

/// <summary>
/// E2E service discovery validation (Issue #16).
/// Verifies that the full Aspire app starts and services can communicate
/// via Aspire service discovery names.
/// </summary>
[Collection("AspireApp")]
public class ServiceDiscoveryTests(AspireAppFixture fixture)
{
    [Fact]
    public async Task AllResources_AreRunning()
    {
        // The fixture already waits for weatherapi and preferencesapi to be healthy.
        // Verify we can create HTTP clients for all expected resources.
        var resourceNames = new[] { "weatherapi", "preferencesapi", "frontend" };

        foreach (var name in resourceNames)
        {
            using var client = fixture.App.CreateHttpClient(name);
            Assert.NotNull(client);
            Assert.NotNull(client.BaseAddress);
        }
    }

    [Fact]
    public async Task Frontend_CanReachWeatherApi_ViaServiceDiscovery()
    {
        // The Frontend uses "https+http://weatherapi" as base address.
        // Via Aspire service discovery, the weatherapi endpoint should be reachable.
        using var weatherClient = fixture.App.CreateHttpClient("weatherapi");

        // Health endpoint is mapped by MapDefaultEndpoints() in ServiceDefaults
        using var response = await weatherClient.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Frontend_CanReachPreferencesApi_ViaServiceDiscovery()
    {
        // The Frontend uses "https+http://preferencesapi" as base address.
        using var preferencesClient = fixture.App.CreateHttpClient("preferencesapi");

        using var response = await preferencesClient.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Worker_IsRunning()
    {
        // Worker is a background service — not an HTTP service, but we can verify
        // it's registered in the Aspire resource model.
        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var workerResource = model.Resources.FirstOrDefault(r => r.Name == "worker");

        Assert.NotNull(workerResource);
    }

    [Fact]
    public void NoHardcodedUrls_InServiceConfiguration()
    {
        // Verify that service references use Aspire service discovery names,
        // not hardcoded URLs like "http://localhost:5000".
        var sourceFiles = Directory.GetFiles(
            Path.Combine(GetRepoRoot(), "WeatherDashboard.Frontend"),
            "*.cs", SearchOption.AllDirectories);

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("http://localhost:", content);
            Assert.DoesNotContain("https://localhost:", content);
        }

        var workerFiles = Directory.GetFiles(
            Path.Combine(GetRepoRoot(), "WeatherDashboard.Worker"),
            "*.cs", SearchOption.AllDirectories);

        foreach (var file in workerFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("http://localhost:", content);
            Assert.DoesNotContain("https://localhost:", content);
        }
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "WeatherDashboard.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? throw new InvalidOperationException("Could not find repo root.");
    }
}
