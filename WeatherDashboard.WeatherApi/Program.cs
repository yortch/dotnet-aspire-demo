using WeatherDashboard.WeatherApi.Endpoints;
using WeatherDashboard.WeatherApi.HealthChecks;
using WeatherDashboard.WeatherApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis", configureOptions: options =>
{
    // Accept Aspire Redis container's self-signed TLS certificate for local development
    options.CertificateValidation += (_, _, _, _) => true;
});

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<OpenWeatherMapService>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

builder.Services.AddSingleton<WeatherCacheService>();

// Health check for OpenWeatherMap API reachability (Redis health check is auto-registered by Aspire)
builder.Services.AddHealthChecks()
    .AddCheck<OpenWeatherMapHealthCheck>("openweathermap");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWeatherEndpoints();

app.Run();