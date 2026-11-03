public sealed record SemVersion(int Major, int Minor, int Patch, string? Prerelease = null) : IComparable<SemVersion>
{
    public static SemVersion Parse(string text) => throw new NotImplementedException();

    public int CompareTo(SemVersion? other) => throw new NotImplementedException();

    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(SemVersion a, SemVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(SemVersion a, SemVersion b) => a.CompareTo(b) >= 0;
}

public static class VersionRange
{
    public static bool Satisfies(string range, string version) => throw new NotImplementedException();

    public static string? Resolve(string range, IEnumerable<string> available) => throw new NotImplementedException();
}
