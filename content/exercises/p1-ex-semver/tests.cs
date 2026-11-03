using LearnHarness;

public class Tests
{
    private static SemVersion V(string s) => SemVersion.Parse(s);

    [Test("تحليل الإصدار مع الأجزاء الناقصة و prerelease و build", "Parse with missing parts, prerelease and build")]
    public void Parses()
    {
        Assert.Equal(new SemVersion(2, 1, 0), V("2.1"));
        Assert.Equal(new SemVersion(3, 0, 0), V("3"));
        Assert.Equal(new SemVersion(1, 2, 3, "rc.1"), V("1.2.3-rc.1"));
        Assert.Equal(new SemVersion(1, 2, 3), V("1.2.3+sha.abc123"));
    }

    [Test("النصوص غير الصالحة ترمي FormatException", "Invalid text throws FormatException")]
    public void Rejects_invalid()
    {
        Assert.Throws<FormatException>(() => V("1.x.0"));
        Assert.Throws<FormatException>(() => V("1.2.3.4"));
        Assert.Throws<FormatException>(() => V("1.0.0-"));
    }

    [Test("المقارنة رقمية وليست نصية: 1.10.0 > 1.9.0", "Numeric, not textual: 1.10.0 > 1.9.0")]
    public void Numeric_comparison()
    {
        Assert.True(V("1.10.0") > V("1.9.0"));
        Assert.True(V("2.0.0") > V("1.99.99"));
        Assert.Equal(0, V("1.0").CompareTo(V("1.0.0")));
    }

    [Test("الـ prerelease أقل من النسخة الرسمية", "Prerelease is lower than the release")]
    public void Prerelease_lower() => Assert.True(V("1.0.0-beta") < V("1.0.0"));

    [Test("ترتيب الـ prerelease حسب قواعد SemVer", "Prerelease ordering follows SemVer rules")]
    public void Prerelease_ordering()
    {
        var expected = new[] { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0" };
        var shuffled = new[] { "1.0.0-beta.11", "1.0.0", "1.0.0-alpha.beta", "1.0.0-rc.1", "1.0.0-alpha", "1.0.0-beta.2", "1.0.0-alpha.1", "1.0.0-beta" };
        Assert.SequenceEqual(expected, shuffled.Select(V).Order().Select(v => v.ToString()));
    }

    [Test("النطاقات: حد أدنى، إصدار محدد، وحدود شاملة ومستثناة", "Ranges: minimum, exact, inclusive and exclusive bounds")]
    public void Ranges()
    {
        Assert.True(VersionRange.Satisfies("1.0", "1.0.0"));
        Assert.True(VersionRange.Satisfies("1.0", "5.2.0"));
        Assert.False(VersionRange.Satisfies("1.0", "0.9.9"));

        Assert.True(VersionRange.Satisfies("[1.2.0]", "1.2.0"));
        Assert.False(VersionRange.Satisfies("[1.2.0]", "1.2.1"));

        Assert.True(VersionRange.Satisfies("[1.0,2.0)", "1.0.0"));
        Assert.True(VersionRange.Satisfies("[1.0,2.0)", "1.10.0"));
        Assert.False(VersionRange.Satisfies("[1.0,2.0)", "2.0.0"));
        Assert.False(VersionRange.Satisfies("(1.0,2.0]", "1.0.0"));
        Assert.True(VersionRange.Satisfies("(1.0,2.0]", "2.0.0"));
    }

    [Test("نطاقات مفتوحة من جهة", "Half-open ranges")]
    public void Open_ranges()
    {
        Assert.True(VersionRange.Satisfies("(,2.0]", "0.1.0"));
        Assert.False(VersionRange.Satisfies("(,2.0]", "2.0.1"));
        Assert.True(VersionRange.Satisfies("(1.0,)", "99.0.0"));
        Assert.False(VersionRange.Satisfies("(1.0,)", "1.0.0"));
    }

    [Test("Resolve يختار أقل إصدار مناسب (قاعدة NuGet)", "Resolve picks the LOWEST applicable version (NuGet rule)")]
    public void Resolve_lowest()
    {
        var available = new[] { "13.0.3", "12.0.1", "9.0.1", "13.0.1", "12.0.3" };
        Assert.Equal("12.0.1", VersionRange.Resolve("12.0", available));
        Assert.Equal("9.0.1", VersionRange.Resolve("[9.0,13.0)", available));
        Assert.Equal("13.0.1", VersionRange.Resolve("(12.0.3,)", available));
        Assert.Null(VersionRange.Resolve("[20.0,)", available));
    }
}
