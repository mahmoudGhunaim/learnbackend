public interface IResourceLog
{
    void Write(string message);
}

// TODO: طبّق الواجهة IDisposable
public sealed class DatabaseConnection
{
    private readonly string _name;
    private readonly IResourceLog _log;

    public DatabaseConnection(string name, IResourceLog log)
    {
        _name = name;
        _log = log;
        throw new NotImplementedException();
    }

    public bool IsDisposed { get; private set; }

    public string Query(string sql) => throw new NotImplementedException();
}

public static class ConnectionWork
{
    public static IReadOnlyList<string> RunQueries(string name, IResourceLog log, IEnumerable<string> queries)
        => throw new NotImplementedException();

    public static void OpenTwo(IResourceLog log) => throw new NotImplementedException();
}
