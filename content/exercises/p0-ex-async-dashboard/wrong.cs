// Realistic mistake: await inside the loop makes every call wait for the previous one,
// and the timeout is never applied.
public interface IWeatherService
{
    Task<int> GetTemperatureAsync(string city);
}

public interface INewsService
{
    Task<IReadOnlyList<string>> GetHeadlinesAsync();
}

public record Dashboard(IReadOnlyDictionary<string, int> Temperatures, IReadOnlyList<string> Headlines);

public class DashboardBuilder(IWeatherService weather, INewsService news)
{
    public async Task<Dashboard> BuildAsync(IEnumerable<string> cities)
    {
        var map = new Dictionary<string, int>();
        foreach (var city in cities)
            map[city] = await weather.GetTemperatureAsync(city);
        var headlines = await news.GetHeadlinesAsync();
        return new Dashboard(map, headlines);
    }

    public async Task<int?> TryGetTemperatureAsync(string city, TimeSpan timeout)
    {
        try { return await weather.GetTemperatureAsync(city); }
        catch (Exception) { return null; }
    }
}
