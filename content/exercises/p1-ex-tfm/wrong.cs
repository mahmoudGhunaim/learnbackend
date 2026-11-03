// Realistic mistakes: checks the "net" prefix first (so netstandard2.0 is parsed as .NET),
// and assumes .NET Standard 2.1 works on .NET Framework like 2.0 does.
public enum FrameworkFamily { DotNet, DotNetFramework, DotNetStandard, DotNetCore }

public record TargetFramework(FrameworkFamily Family, Version Version, string? Platform = null);

public static class TfmParser
{
    public static TargetFramework Parse(string tfm)
    {
        var text = tfm.Trim().ToLowerInvariant();
        if (text.StartsWith("net") && text.Contains('.'))
        {
            var parts = text.Split('-', 2);
            var digits = new string(parts[0].Where(c => char.IsDigit(c) || c == '.').ToArray());
            return new TargetFramework(FrameworkFamily.DotNet, Version.Parse(digits), parts.Length > 1 ? parts[1] : null);
        }
        if (text.StartsWith("net"))
            return new TargetFramework(FrameworkFamily.DotNetFramework, Version.Parse(string.Join('.', text[3..].ToCharArray())));
        throw new FormatException();
    }

    public static bool CanReference(string projectTfm, string libraryTfm)
    {
        var project = Parse(projectTfm);
        var lib = Parse(libraryTfm);
        if (lib.Family == FrameworkFamily.DotNetStandard) return true;
        return project.Family == lib.Family && project.Version >= lib.Version;
    }
}
