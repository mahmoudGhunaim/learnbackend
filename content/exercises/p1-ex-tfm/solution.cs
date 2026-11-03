public enum FrameworkFamily { DotNet, DotNetFramework, DotNetStandard, DotNetCore }

public record TargetFramework(FrameworkFamily Family, Version Version, string? Platform = null);

public static class TfmParser
{
    public static TargetFramework Parse(string tfm)
    {
        ArgumentNullException.ThrowIfNull(tfm);
        var text = tfm.Trim().ToLowerInvariant();

        // Longest prefixes first: "netstandard" and "netcoreapp" also start with "net".
        if (text.StartsWith("netstandard"))
            return new TargetFramework(FrameworkFamily.DotNetStandard, ParseDotted(text["netstandard".Length..], tfm));
        if (text.StartsWith("netcoreapp"))
            return new TargetFramework(FrameworkFamily.DotNetCore, ParseDotted(text["netcoreapp".Length..], tfm));
        if (!text.StartsWith("net"))
            throw new FormatException($"'{tfm}' is not a valid target framework moniker.");

        var parts = text["net".Length..].Split('-', 2);
        var versionText = parts[0];
        var platform = parts.Length == 2 && parts[1].Length > 0 ? parts[1] : null;

        if (versionText.Contains('.'))
        {
            var version = ParseDotted(versionText, tfm);
            if (version.Major < 5) throw new FormatException($"'{tfm}': dotted 'net' versions start at 5.0.");
            return new TargetFramework(FrameworkFamily.DotNet, version, platform);
        }

        // .NET Framework: "net48" → 4.8, "net472" → 4.7.2 (one digit per version part)
        if (platform is not null || versionText.Length is < 2 or > 3 || !versionText.All(char.IsAsciiDigit))
            throw new FormatException($"'{tfm}' is not a valid .NET Framework moniker.");
        return new TargetFramework(FrameworkFamily.DotNetFramework, Version.Parse(string.Join('.', versionText.ToCharArray())));
    }

    public static bool CanReference(string projectTfm, string libraryTfm)
    {
        var project = Parse(projectTfm);
        var lib = Parse(libraryTfm);
        var p = project.Family;

        return lib.Family switch
        {
            FrameworkFamily.DotNet =>
                p == FrameworkFamily.DotNet && project.Version >= lib.Version
                && (lib.Platform is null || lib.Platform == project.Platform),

            FrameworkFamily.DotNetFramework =>
                p == FrameworkFamily.DotNetFramework && project.Version >= lib.Version,

            FrameworkFamily.DotNetCore =>
                p == FrameworkFamily.DotNet || (p == FrameworkFamily.DotNetCore && project.Version >= lib.Version),

            FrameworkFamily.DotNetStandard when lib.Version >= new Version(2, 1) =>
                p == FrameworkFamily.DotNet
                || (p == FrameworkFamily.DotNetCore && project.Version >= new Version(3, 0))
                || (p == FrameworkFamily.DotNetStandard && project.Version >= lib.Version),

            FrameworkFamily.DotNetStandard when lib.Version >= new Version(2, 0) =>
                p == FrameworkFamily.DotNet
                || (p == FrameworkFamily.DotNetCore && project.Version >= new Version(2, 0))
                || (p == FrameworkFamily.DotNetFramework && project.Version >= new Version(4, 6, 1))
                || (p == FrameworkFamily.DotNetStandard && project.Version >= lib.Version),

            FrameworkFamily.DotNetStandard =>
                p is FrameworkFamily.DotNet or FrameworkFamily.DotNetCore
                || (p == FrameworkFamily.DotNetFramework && project.Version >= new Version(4, 5))
                || (p == FrameworkFamily.DotNetStandard && project.Version >= lib.Version),

            _ => false,
        };
    }

    private static Version ParseDotted(string text, string original) =>
        Version.TryParse(text, out var v) ? v : throw new FormatException($"'{original}' has an invalid version.");
}
