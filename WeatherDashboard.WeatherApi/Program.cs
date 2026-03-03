using WeatherDashboard.WeatherApi.Endpoints;
using WeatherDashboard.WeatherApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<OpenWeatherMapService>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

builder.Services.AddSingleton<WeatherCacheService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWeatherEndpoints();

app.Run();
