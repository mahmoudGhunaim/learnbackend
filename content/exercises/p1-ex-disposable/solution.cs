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
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL is required.", nameof(sql));
        return $"{_name}: {sql}";
    }

    // Idempotent: calling Dispose twice must be harmless.
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        _log.Write($"close {_name}");
    }
}

public static class ConnectionWork
{
    public static IReadOnlyList<string> RunQueries(string name, IResourceLog log, IEnumerable<string> queries)
    {
        // Disposed when the method exits: normally OR because of an exception.
        using var connection = new DatabaseConnection(name, log);
        return queries.Select(connection.Query).ToList();   // ToList runs the queries BEFORE disposal
    }

    public static void OpenTwo(IResourceLog log)
    {
        using var a = new DatabaseConnection("A", log);
        using var b = new DatabaseConnection("B", log);
        a.Query("SELECT 1");
        b.Query("SELECT 1");
    }   // disposed in reverse order: B, then A
}
