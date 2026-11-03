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
    public static ProjectInfo Read(string xml) => throw new NotImplementedException();

    public static bool IsWebProject(ProjectInfo info) => throw new NotImplementedException();
}
