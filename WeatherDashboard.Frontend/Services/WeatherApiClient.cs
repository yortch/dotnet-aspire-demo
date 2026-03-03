using System.Net.Http.Json;
using WeatherDashboard.Frontend.Models;

namespace WeatherDashboard.Frontend.Services;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<CurrentWeather?> GetCurrentWeatherAsync(string city)
    {
        return await httpClient.GetFromJsonAsync<CurrentWeather>($"/api/weather/{Uri.EscapeDataString(city)}");
    }

    public async Task<Forecast?> GetForecastAsync(string city)
    {
        return await httpClient.GetFromJsonAsync<Forecast>($"/api/weather/{Uri.EscapeDataString(city)}/forecast");
    }
}
