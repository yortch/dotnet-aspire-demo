namespace WeatherDashboard.Frontend.Models;

public record ForecastDay
{
    public DateTime Date { get; init; }
    public double HighF { get; init; }
    public double LowF { get; init; }
    public double HighC { get; init; }
    public double LowC { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
}

public record Forecast
{
    public string City { get; init; } = string.Empty;
    public List<ForecastDay> Days { get; init; } = [];
    public DateTime RetrievedAt { get; init; }
}
