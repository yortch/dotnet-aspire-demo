var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithoutHttpsCertificate();
var postgres = builder.AddPostgres("postgres").AddDatabase("preferencesdb");

var weatherapi = builder.AddProject<Projects.WeatherDashboard_WeatherApi>("weatherapi")
    .WithReference(redis)
    .WaitFor(redis);

var preferencesapi = builder.AddProject<Projects.WeatherDashboard_PreferencesApi>("preferencesapi")
    .WithReference(postgres)
    .WaitFor(postgres);

var frontend = builder.AddProject<Projects.WeatherDashboard_Frontend>("frontend")
    .WithReference(weatherapi)
    .WithReference(preferencesapi)
    .WaitFor(weatherapi)
    .WaitFor(preferencesapi);

var worker = builder.AddProject<Projects.WeatherDashboard_Worker>("worker")
    .WithReference(weatherapi)
    .WithReference(preferencesapi)
    .WaitFor(weatherapi)
    .WaitFor(preferencesapi);

builder.Build().Run();
