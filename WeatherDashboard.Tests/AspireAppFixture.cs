namespace WeatherDashboard.Tests;

/// <summary>
/// Shared fixture that starts the Aspire distributed application once for all test classes.
/// </summary>
public sealed class AspireAppFixture : IAsyncLifetime
{
    private Aspire.Hosting.DistributedApplication? _app;
    private IDistributedApplicationTestingBuilder? _appHost;

    public Aspire.Hosting.DistributedApplication App => _app ?? throw new InvalidOperationException("App not started.");

    public async Task InitializeAsync()
    {
        _appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WeatherDashboard_AppHost>();

        _appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        _app = await _appHost.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        await _app.StartAsync();

        // Wait for core resources to be running
        await resourceNotificationService.WaitForResourceHealthyAsync("weatherapi").WaitAsync(TimeSpan.FromSeconds(120));
        await resourceNotificationService.WaitForResourceHealthyAsync("preferencesapi").WaitAsync(TimeSpan.FromSeconds(120));
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition("AspireApp")]
public class AspireAppCollection : ICollectionFixture<AspireAppFixture>;
