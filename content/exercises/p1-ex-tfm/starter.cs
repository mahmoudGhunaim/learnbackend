public enum FrameworkFamily { DotNet, DotNetFramework, DotNetStandard, DotNetCore }

public record TargetFramework(FrameworkFamily Family, Version Version, string? Platform = null);

public static class TfmParser
{
    // "net10.0" → (DotNet, 10.0)   "net48" → (DotNetFramework, 4.8)   "netstandard2.0" → (DotNetStandard, 2.0)
    public static TargetFramework Parse(string tfm)
    {
        throw new NotImplementedException();
    }

    // هل يمكن لمشروع يستهدف projectTfm أن يستخدم مكتبة تستهدف libraryTfm؟
    public static bool CanReference(string projectTfm, string libraryTfm)
    {
        throw new NotImplementedException();
    }
}
