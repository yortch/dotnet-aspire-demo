namespace WeatherDashboard.Frontend.Models;

public record CurrentWeather
{
    public string City { get; init; } = string.Empty;
    public double TemperatureF { get; init; }
    public double TemperatureC { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int Humidity { get; init; }
    public double WindSpeedMph { get; init; }
    public DateTime RetrievedAt { get; init; }
}
