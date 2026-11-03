using LearnHarness;

/// <summary>Fake services that record how many calls are in flight at the same moment.</summary>
public class Tracker
{
    private int _current;
    public int MaxConcurrent { get; private set; }
    public int Calls { get; private set; }

    public async Task<T> Track<T>(Func<T> result, int delayMs)
    {
        lock (this) { Calls++; _current++; MaxConcurrent = Math.Max(MaxConcurrent, _current); }
        try { await Task.Delay(delayMs); return result(); }
        finally { lock (this) _current--; }
    }
}

public class FakeWeather(Tracker tracker, int delayMs = 150) : IWeatherService
{
    public Task<int> GetTemperatureAsync(string city) => tracker.Track(() => city switch
    {
        "Amman" => 18,
        "Cairo" => 27,
        "Riyadh" => 33,
        "Broken" => throw new InvalidOperationException("Service down"),
        _ => 20,
    }, delayMs);
}

public class FakeNews(Tracker tracker) : INewsService
{
    public Task<IReadOnlyList<string>> GetHeadlinesAsync() =>
        tracker.Track<IReadOnlyList<string>>(() => new[] { ".NET 10 released", "C# 14 is here" }, 150);
}

public class Tests
{
    [Test("النتيجة تحتوي حرارة كل مدينة والأخبار", "Result contains every city and the headlines")]
    public async Task Builds_dashboard()
    {
        var t = new Tracker();
        var d = await new DashboardBuilder(new FakeWeather(t), new FakeNews(t)).BuildAsync(new[] { "Amman", "Cairo", "Riyadh" });
        Assert.Equal(3, d.Temperatures.Count);
        Assert.Equal(18, d.Temperatures["Amman"]);
        Assert.Equal(27, d.Temperatures["Cairo"]);
        Assert.Equal(33, d.Temperatures["Riyadh"]);
        Assert.Equal(2, d.Headlines.Count);
    }

    [Test("كل الطلبات تعمل في نفس الوقت (Task.WhenAll)", "All calls run concurrently (Task.WhenAll)")]
    public async Task Calls_run_concurrently()
    {
        var t = new Tracker();
        await new DashboardBuilder(new FakeWeather(t), new FakeNews(t)).BuildAsync(new[] { "Amman", "Cairo", "Riyadh" });
        Assert.Equal(4, t.MaxConcurrent, "3 weather calls + 1 news call should be in flight together");
    }

    [Test("كل خدمة تُستدعى مرة واحدة فقط", "Each service is called exactly once per item")]
    public async Task No_duplicate_calls()
    {
        var t = new Tracker();
        await new DashboardBuilder(new FakeWeather(t), new FakeNews(t)).BuildAsync(new[] { "Amman", "Cairo" });
        Assert.Equal(3, t.Calls, "Watch out for deferred execution starting tasks twice");
    }

    [Test("بلا مدن: قاموس فارغ والأخبار موجودة", "No cities: empty dictionary but headlines present")]
    public async Task Empty_cities()
    {
        var t = new Tracker();
        var d = await new DashboardBuilder(new FakeWeather(t), new FakeNews(t)).BuildAsync(Array.Empty<string>());
        Assert.Equal(0, d.Temperatures.Count);
        Assert.Equal(2, d.Headlines.Count);
    }

    [Test("TryGetTemperatureAsync يرجع القيمة عند النجاح", "TryGetTemperatureAsync returns the value on success")]
    public async Task Try_success()
    {
        var b = new DashboardBuilder(new FakeWeather(new Tracker(), 10), new FakeNews(new Tracker()));
        Assert.Equal((int?)27, await b.TryGetTemperatureAsync("Cairo", TimeSpan.FromSeconds(1)));
    }

    [Test("TryGetTemperatureAsync يرجع null عند الاستثناء", "TryGetTemperatureAsync returns null when the service throws")]
    public async Task Try_exception()
    {
        var b = new DashboardBuilder(new FakeWeather(new Tracker(), 10), new FakeNews(new Tracker()));
        Assert.Null(await b.TryGetTemperatureAsync("Broken", TimeSpan.FromSeconds(1)));
    }

    [Test("TryGetTemperatureAsync يرجع null عند انتهاء المهلة", "TryGetTemperatureAsync returns null on timeout")]
    public async Task Try_timeout()
    {
        var b = new DashboardBuilder(new FakeWeather(new Tracker(), 1500), new FakeNews(new Tracker()));
        var started = DateTime.UtcNow;
        Assert.Null(await b.TryGetTemperatureAsync("Amman", TimeSpan.FromMilliseconds(100)));
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromMilliseconds(1000), "Should give up after ~100 ms, not wait for the slow service");
    }
}
