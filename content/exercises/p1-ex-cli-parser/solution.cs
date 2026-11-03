public record CliCommand(
    string Verb,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string?> Options);

public static class DotnetCli
{
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["n"] = "name",
        ["o"] = "output",
        ["f"] = "framework",
        ["c"] = "configuration",
        ["v"] = "verbosity",
    };

    public static CliCommand Parse(string[] args)
    {
        if (args is null || args.Length == 0)
            throw new ArgumentException("A command is required, e.g. 'dotnet build'.", nameof(args));

        var arguments = new List<string>();
        var options = new Dictionary<string, string?>();

        // Index-based loop: an option may "consume" the next item as its value.
        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-'))
            {
                arguments.Add(token);
                continue;
            }

            var key = token.TrimStart('-');
            string? value = null;

            var eq = key.Split('=', 2);
            if (eq.Length == 2)
            {
                key = eq[0];
                value = eq[1];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            {
                value = args[++i];
            }

            key = key.ToLowerInvariant();
            if (!token.StartsWith("--") && Aliases.TryGetValue(key, out var full)) key = full;
            options[key] = value;
        }

        return new CliCommand(args[0].ToLowerInvariant(), arguments, options);
    }

    public static bool RequiresProject(CliCommand command) =>
        command.Verb is "build" or "run" or "test" or "publish" or "restore" or "watch" or "clean" or "pack" or "add";
}
