public record CliCommand(
    string Verb,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string?> Options);

public static class DotnetCli
{
    // ["new", "webapi", "-n", "Shop", "--framework", "net10.0", "--no-https"]
    //   → Verb "new", Arguments ["webapi"], Options { name: "Shop", framework: "net10.0", no-https: null }
    public static CliCommand Parse(string[] args) => throw new NotImplementedException();

    public static bool RequiresProject(CliCommand command) => throw new NotImplementedException();
}
