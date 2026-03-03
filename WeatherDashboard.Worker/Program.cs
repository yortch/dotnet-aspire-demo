using WeatherDashboard.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("weatherapi", client =>
    client.BaseAddress = new Uri("https+http://weatherapi"));
builder.Services.AddHttpClient("preferencesapi", client =>
    client.BaseAddress = new Uri("https+http://preferencesapi"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
