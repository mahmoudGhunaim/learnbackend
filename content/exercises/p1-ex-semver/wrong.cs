// Realistic mistake: comparing versions as STRINGS ("1.10.0" < "1.9.0" alphabetically),
// and resolving to the HIGHEST version (npm habit) instead of NuGet's lowest applicable.
public sealed record SemVersion(int Major, int Minor, int Patch, string? Prerelease = null) : IComparable<SemVersion>
{
    public static SemVersion Parse(string text)
    {
        var main = text.Split('+')[0].Split('-', 2);
        var parts = main[0].Split('.').Select(int.Parse).Concat(new[] { 0, 0 }).ToArray();
        return new SemVersion(parts[0], parts[1], parts[2], main.Length == 2 ? main[1] : null);
    }

    public int CompareTo(SemVersion? other) => string.CompareOrdinal(ToString(), other?.ToString());

    public override string ToString() => Prerelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(SemVersion a, SemVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(SemVersion a, SemVersion b) => a.CompareTo(b) >= 0;
}

public static class VersionRange
{
    public static bool Satisfies(string range, string version)
    {
        var v = SemVersion.Parse(version);
        var inner = range.Trim('[', ']', '(', ')');
        var bounds = inner.Split(',');
        if (bounds.Length == 1) return v >= SemVersion.Parse(bounds[0]);
        return (bounds[0] == "" || v >= SemVersion.Parse(bounds[0])) && (bounds[1] == "" || v <= SemVersion.Parse(bounds[1]));
    }

    public static string? Resolve(string range, IEnumerable<string> available) =>
        available.Where(v => Satisfies(range, v)).Select(SemVersion.Parse).OrderDescending().FirstOrDefault()?.ToString();
}
