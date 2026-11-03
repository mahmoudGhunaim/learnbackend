// Realistic mistakes: only reads <TargetFramework> (singular), only reads the Version
// attribute, and uses Path directly on a backslash path (breaks on Linux/macOS).
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
        var root = XDocument.Parse(xml).Root!;
        var tf = root.Descendants("TargetFramework").FirstOrDefault()?.Value;
        return new ProjectInfo(
            root.Attribute("Sdk")?.Value ?? "",
            tf is null ? new List<string>() : new List<string> { tf },
            root.Descendants("OutputType").FirstOrDefault()?.Value ?? "Exe",
            root.Descendants("Nullable").FirstOrDefault()?.Value == "enable",
            root.Descendants("ImplicitUsings").FirstOrDefault()?.Value == "enable",
            root.Descendants("PackageReference").Select(p => new PackageRef(p.Attribute("Include")!.Value, p.Attribute("Version")?.Value ?? "")).ToList(),
            root.Descendants("ProjectReference").Select(p => p.Attribute("Include")!.Value.Split('/').Last().Replace(".csproj", "")).ToList());
    }

    public static bool IsWebProject(ProjectInfo info) => info.Sdk.Contains("Web");
}
