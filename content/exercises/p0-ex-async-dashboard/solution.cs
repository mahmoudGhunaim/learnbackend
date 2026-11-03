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
        var cityList = cities.ToList();

        // 1) Start every call WITHOUT awaiting: they all run at the same time.
        //    ToList() matters: without it, deferred execution would start the calls again on every enumeration.
        var temperatureTasks = cityList.Select(city => weather.GetTemperatureAsync(city)).ToList();
        var headlinesTask = news.GetHeadlinesAsync();

        // 2) Wait for all of them (like Promise.all).
        var temperatures = await Task.WhenAll(temperatureTasks);
        var headlines = await headlinesTask;

        var map = new Dictionary<string, int>();
        for (var i = 0; i < cityList.Count; i++)
            map[cityList[i]] = temperatures[i];

        return new Dashboard(map, headlines);
    }

    public async Task<int?> TryGetTemperatureAsync(string city, TimeSpan timeout)
    {
        try
        {
            return await weather.GetTemperatureAsync(city).WaitAsync(timeout);
        }
        catch (Exception)   // TimeoutException or whatever the service threw
        {
            return null;
        }
    }
}
