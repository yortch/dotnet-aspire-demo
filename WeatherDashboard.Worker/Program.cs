using WeatherDashboard.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("weatherapi", client =>
    client.BaseAddress = new Uri("https+http://weatherapi"));
builder.Services.AddHttpClient("preferencesapi", client =>
    client.BaseAddress = new Uri("https+http://preferencesapi"));

builder.Services.AddSingleton<WeatherRefreshHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<WeatherRefreshHealthCheck>("weather-refresh");

builder.Services.AddHostedService<WeatherRefreshWorker>();

var host = builder.Build();
host.Run();
