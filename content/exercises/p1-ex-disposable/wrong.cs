// Realistic mistakes: Dispose is not idempotent, and Dispose is called manually at the end,
// so it is skipped when an exception happens. OpenTwo also closes in the wrong order.
public interface IResourceLog
{
    void Write(string message);
}

public sealed class DatabaseConnection : IDisposable
{
    private readonly string _name;
    private readonly IResourceLog _log;

    public DatabaseConnection(string name, IResourceLog log)
    {
        _name = name;
        _log = log;
        _log.Write($"open {_name}");
    }

    public bool IsDisposed { get; private set; }

    public string Query(string sql)
    {
        if (IsDisposed) throw new ObjectDisposedException(_name);
        if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL is required.");
        return $"{_name}: {sql}";
    }

    public void Dispose()
    {
        IsDisposed = true;
        _log.Write($"close {_name}");
    }
}

public static class ConnectionWork
{
    public static IReadOnlyList<string> RunQueries(string name, IResourceLog log, IEnumerable<string> queries)
    {
        var connection = new DatabaseConnection(name, log);
        var results = queries.Select(connection.Query).ToList();
        connection.Dispose();
        return results;
    }

    public static void OpenTwo(IResourceLog log)
    {
        var a = new DatabaseConnection("A", log);
        var b = new DatabaseConnection("B", log);
        a.Query("SELECT 1");
        b.Query("SELECT 1");
        a.Dispose();
        b.Dispose();
    }
}
