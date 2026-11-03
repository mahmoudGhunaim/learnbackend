public interface IWeatherService
{
    Task<int> GetTemperatureAsync(string city);
}

public interface INewsService
{
    Task<IReadOnlyList<string>> GetHeadlinesAsync();
}

public record Dashboard(IReadOnlyDictionary<string, int> Temperatures, IReadOnlyList<string> Headlines);

// Primary constructor: weather و news متاحتان في كل الصنف
public class DashboardBuilder(IWeatherService weather, INewsService news)
{
    public async Task<Dashboard> BuildAsync(IEnumerable<string> cities)
    {
        // TODO: اجلب كل شيء بالتوازي
        await Task.Yield();
        throw new NotImplementedException();
    }

    public async Task<int?> TryGetTemperatureAsync(string city, TimeSpan timeout)
    {
        await Task.Yield();
        throw new NotImplementedException();
    }
}
