using System.Net;
using System.Text.Json;
using WeatherDashboard.WeatherApi.Models;

namespace WeatherDashboard.WeatherApi.Services;

public class OpenWeatherMapService(HttpClient httpClient, IConfiguration configuration)
{
    private readonly string _apiKey = configuration["OpenWeatherMap:ApiKey"]
        ?? throw new InvalidOperationException("OpenWeatherMap:ApiKey is not configured.");

    public static string NormalizeCity(string city) =>
        city.Trim().ToLowerInvariant().Replace(' ', '-');

    public async Task<CurrentWeather> GetCurrentWeatherAsync(string city)
    {
        var response = await httpClient.GetAsync($"data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=imperial");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"City '{city}' not found.");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenWeatherMap API error: {response.StatusCode}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new CurrentWeather
        {
            City = json.GetProperty("name").GetString() ?? city,
            TemperatureF = json.GetProperty("main").GetProperty("temp").GetDouble(),
            TemperatureC = (json.GetProperty("main").GetProperty("temp").GetDouble() - 32) * 5.0 / 9.0,
            Conditions = json.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty,
            Icon = json.GetProperty("weather")[0].GetProperty("icon").GetString() ?? string.Empty,
            Humidity = json.GetProperty("main").GetProperty("humidity").GetInt32(),
            WindSpeedMph = json.GetProperty("wind").GetProperty("speed").GetDouble(),
            RetrievedAt = DateTime.UtcNow
        };
    }

    public async Task<Forecast> GetForecastAsync(string city)
    {
        var response = await httpClient.GetAsync($"data/2.5/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=imperial");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"City '{city}' not found.");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenWeatherMap API error: {response.StatusCode}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var cityName = json.GetProperty("city").GetProperty("name").GetString() ?? city;

        // Group 3-hour intervals into daily highs/lows
        var days = json.GetProperty("list").EnumerateArray()
            .GroupBy(item => DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).Date)
            .Take(5)
            .Select(group =>
            {
                var temps = group.Select(i => i.GetProperty("main").GetProperty("temp").GetDouble()).ToList();
                var highF = temps.Max();
                var lowF = temps.Min();
                var midday = group.OrderBy(i => Math.Abs(
                    DateTimeOffset.FromUnixTimeSeconds(i.GetProperty("dt").GetInt64()).Hour - 12)).First();

                return new ForecastDay
                {
                    Date = group.Key,
                    HighF = Math.Round(highF, 1),
                    LowF = Math.Round(lowF, 1),
                    HighC = Math.Round((highF - 32) * 5.0 / 9.0, 1),
                    LowC = Math.Round((lowF - 32) * 5.0 / 9.0, 1),
                    Conditions = midday.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty,
                    Icon = midday.GetProperty("weather")[0].GetProperty("icon").GetString() ?? string.Empty
                };
            })
            .ToList();

        return new Forecast
        {
            City = cityName,
            Days = days,
            RetrievedAt = DateTime.UtcNow
        };
    }
}
