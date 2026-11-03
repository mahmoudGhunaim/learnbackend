// Realistic mistake: a foreach loop can't look ahead, so option values end up as arguments;
// short aliases are ignored and the verb keeps its original casing.
public record CliCommand(
    string Verb,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string?> Options);

public static class DotnetCli
{
    public static CliCommand Parse(string[] args)
    {
        var arguments = new List<string>();
        var options = new Dictionary<string, string?>();
        foreach (var token in args.Skip(1))
        {
            if (token.StartsWith("--"))
            {
                var parts = token[2..].Split('=', 2);
                options[parts[0]] = parts.Length == 2 ? parts[1] : null;
            }
            else
            {
                arguments.Add(token);
            }
        }
        return new CliCommand(args[0], arguments, options);
    }

    public static bool RequiresProject(CliCommand command) => command.Verb != "new";
}
