namespace LearnBackend.Tests.Infrastructure;

/// <summary>Locates the repository folders from the test's bin directory.</summary>
public static class TestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();
    public static string ContentRoot => Path.Combine(RepoRoot, "content");
    public static string ApiProject => Path.Combine(RepoRoot, "Backend", "LearnBackend.Api");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LearnBackend.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find LearnBackend.slnx above the test folder.");
    }
}
