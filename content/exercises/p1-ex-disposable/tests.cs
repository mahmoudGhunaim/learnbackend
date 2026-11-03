using LearnHarness;

public class ListLog : IResourceLog
{
    public List<string> Lines { get; } = new();
    public void Write(string message) => Lines.Add(message);
}

public class Tests
{
    [Test("الصنف يطبّق IDisposable", "The class implements IDisposable")]
    public void Implements_IDisposable()
    {
        object c = new DatabaseConnection("db", new ListLog());
        Assert.True(c is IDisposable);
    }

    [Test("الفتح والاستعلام والإغلاق في الـ log", "Open, query and close are logged")]
    public void Logs_lifecycle()
    {
        var log = new ListLog();
        var c = new DatabaseConnection("db", log);
        Assert.Equal("db: SELECT 1", c.Query("SELECT 1"));
        ((IDisposable)(object)c).Dispose();
        Assert.SequenceEqual(new[] { "open db", "close db" }, log.Lines);
        Assert.True(c.IsDisposed);
    }

    [Test("استدعاء Dispose مرتين يغلق مرة واحدة", "Calling Dispose twice closes only once")]
    public void Dispose_is_idempotent()
    {
        var log = new ListLog();
        var c = (IDisposable)(object)new DatabaseConnection("db", log);
        c.Dispose();
        c.Dispose();
        Assert.Equal(1, log.Lines.Count(l => l == "close db"));
    }

    [Test("الاستعلام بعد الإغلاق يرمي ObjectDisposedException", "Query after disposal throws ObjectDisposedException")]
    public void Query_after_dispose()
    {
        var c = new DatabaseConnection("db", new ListLog());
        ((IDisposable)(object)c).Dispose();
        Assert.Throws<ObjectDisposedException>(() => c.Query("SELECT 1"));
    }

    [Test("RunQueries يرجع النتائج ويغلق الاتصال", "RunQueries returns results and closes the connection")]
    public void RunQueries_happy_path()
    {
        var log = new ListLog();
        var results = ConnectionWork.RunQueries("shop", log, new[] { "SELECT a", "SELECT b" });
        Assert.SequenceEqual(new[] { "shop: SELECT a", "shop: SELECT b" }, results);
        Assert.SequenceEqual(new[] { "open shop", "close shop" }, log.Lines);
    }

    [Test("الاتصال يُغلق حتى عند حدوث استثناء", "The connection is closed even when an exception happens")]
    public void RunQueries_closes_on_exception()
    {
        var log = new ListLog();
        Assert.Throws<ArgumentException>(() => ConnectionWork.RunQueries("shop", log, new[] { "SELECT a", "" }));
        Assert.SequenceEqual(new[] { "open shop", "close shop" }, log.Lines);
    }

    [Test("using تغلق بالترتيب العكسي", "using disposes in reverse order")]
    public void OpenTwo_reverse_order()
    {
        var log = new ListLog();
        ConnectionWork.OpenTwo(log);
        Assert.SequenceEqual(new[] { "open A", "open B", "close B", "close A" }, log.Lines);
    }
}
