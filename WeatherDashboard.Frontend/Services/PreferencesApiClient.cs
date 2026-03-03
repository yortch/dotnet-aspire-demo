using System.Net.Http.Json;
using WeatherDashboard.Frontend.Models;

namespace WeatherDashboard.Frontend.Services;

public class PreferencesApiClient(HttpClient httpClient)
{
    public async Task<List<UserPreference>> GetUserPreferencesAsync(string userId)
    {
        return await httpClient.GetFromJsonAsync<List<UserPreference>>($"/api/preferences/{Uri.EscapeDataString(userId)}")
            ?? [];
    }

    public async Task<HttpResponseMessage> AddCityAsync(string userId, string city)
    {
        return await httpClient.PostAsJsonAsync($"/api/preferences/{Uri.EscapeDataString(userId)}", new { City = city });
    }

    public async Task RemoveCityAsync(string userId, string city)
    {
        await httpClient.DeleteAsync($"/api/preferences/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(city)}");
    }
}
