using System.IO;
using System.Xml.Linq;

public record PackageRef(string Id, string Version);

public record ProjectInfo(
    string Sdk,
    IReadOnlyList<string> TargetFrameworks,
    string OutputType,
    bool NullableEnabled,
    bool ImplicitUsings,
    IReadOnlyList<PackageRef> Packages,
    IReadOnlyList<string> ProjectReferences);

public static class CsprojReader
{
    public static ProjectInfo Read(string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new FormatException("Empty project file.");

        string? Property(string name) => root.Descendants(name).FirstOrDefault()?.Value.Trim();

        var frameworks = (Property("TargetFrameworks") ?? Property("TargetFramework") ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var packages = root.Descendants("PackageReference")
            .Select(p => new PackageRef(
                p.Attribute("Include")?.Value ?? "",
                p.Attribute("Version")?.Value ?? p.Element("Version")?.Value.Trim() ?? ""))
            .ToList();

        // MSBuild paths use backslashes even on Linux/macOS: normalize before using Path.
        var projectRefs = root.Descendants("ProjectReference")
            .Select(p => p.Attribute("Include")?.Value ?? "")
            .Where(path => path.Length > 0)
            .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
            .ToList();

        var implicitUsings = Property("ImplicitUsings");

        return new ProjectInfo(
            root.Attribute("Sdk")?.Value ?? "",
            frameworks,
            Property("OutputType") ?? "Library",
            string.Equals(Property("Nullable"), "enable", StringComparison.OrdinalIgnoreCase),
            string.Equals(implicitUsings, "enable", StringComparison.OrdinalIgnoreCase)
                || string.Equals(implicitUsings, "true", StringComparison.OrdinalIgnoreCase),
            packages,
            projectRefs);
    }

    public static bool IsWebProject(ProjectInfo info) =>
        string.Equals(info.Sdk, "Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
}
