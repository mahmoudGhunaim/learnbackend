public sealed record SemVersion(int Major, int Minor, int Patch, string? Prerelease = null) : IComparable<SemVersion>
{
    public static SemVersion Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var value = text.Trim();

        var plus = value.IndexOf('+');          // build metadata is ignored for precedence
        if (plus >= 0) value = value[..plus];

        var dash = value.Split('-', 2);
        string? prerelease = dash.Length == 2 ? dash[1] : null;
        if (prerelease is not null && (prerelease.Length == 0 || prerelease.Split('.').Any(p => p.Length == 0)))
            throw new FormatException($"Invalid prerelease in '{text}'.");

        var parts = dash[0].Split('.');
        if (parts.Length is < 1 or > 3) throw new FormatException($"'{text}' must have 1 to 3 numeric parts.");

        var numbers = new int[3];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0 || !parts[i].All(char.IsAsciiDigit))
                throw new FormatException($"'{parts[i]}' is not a valid version number in '{text}'.");
        }
        return new SemVersion(numbers[0], numbers[1], numbers[2], prerelease);
    }

    public int CompareTo(SemVersion? other)
    {
        if (other is null) return 1;
        var c = Major.CompareTo(other.Major);
        if (c != 0) return c;
        c = Minor.CompareTo(other.Minor);
        if (c != 0) return c;
        c = Patch.CompareTo(other.Patch);
        if (c != 0) return c;

        // A release ranks higher than any prerelease of the same version.
        if (Prerelease is null) return other.Prerelease is null ? 0 : 1;
        if (other.Prerelease is null) return -1;

        var a = Prerelease.Split('.');
        var b = other.Prerelease.Split('.');
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            c = CompareIdentifier(a[i], b[i]);
            if (c != 0) return c;
        }
        return a.Length.CompareTo(b.Length);
    }

    private static int CompareIdentifier(string x, string y)
    {
        var xNum = int.TryParse(x, out var xi);
        var yNum = int.TryParse(y, out var yi);
        return (xNum, yNum) switch
        {
            (true, true) => xi.CompareTo(yi),
            (true, false) => -1,                 // numeric identifiers sort before alphanumeric ones
            (false, true) => 1,
            _ => string.CompareOrdinal(x, y),
        };
    }

    public override string ToString() => Prerelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(SemVersion a, SemVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(SemVersion a, SemVersion b) => a.CompareTo(b) >= 0;
}

public static class VersionRange
{
    public static bool Satisfies(string range, string version) => Matches(range.Trim(), SemVersion.Parse(version));

    public static string? Resolve(string range, IEnumerable<string> available) =>
        available
            .Select(SemVersion.Parse)
            .Where(v => Matches(range.Trim(), v))
            .Order()                   // works because SemVersion implements IComparable<SemVersion>
            .FirstOrDefault()          // NuGet's rule: the LOWEST applicable version
            ?.ToString();

    private static bool Matches(string range, SemVersion v)
    {
        // "1.0" → minimum version, inclusive
        if (range[0] is not ('[' or '('))
            return v >= SemVersion.Parse(range);

        var minInclusive = range[0] == '[';
        var maxInclusive = range[^1] == ']';
        if (range[^1] is not (']' or ')')) throw new FormatException($"Invalid range '{range}'.");
        var inner = range[1..^1];

        // "[1.0]" → exactly 1.0
        if (!inner.Contains(','))
            return minInclusive && maxInclusive && v.CompareTo(SemVersion.Parse(inner)) == 0;

        var bounds = inner.Split(',');
        if (bounds.Length != 2) throw new FormatException($"Invalid range '{range}'.");

        if (bounds[0].Trim() is { Length: > 0 } minText)
        {
            var c = v.CompareTo(SemVersion.Parse(minText));
            if (c < 0 || (c == 0 && !minInclusive)) return false;
        }
        if (bounds[1].Trim() is { Length: > 0 } maxText)
        {
            var c = v.CompareTo(SemVersion.Parse(maxText));
            if (c > 0 || (c == 0 && !maxInclusive)) return false;
        }
        return true;
    }
}
