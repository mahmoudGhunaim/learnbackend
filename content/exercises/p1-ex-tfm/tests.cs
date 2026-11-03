using LearnHarness;

public class Tests
{
    [Test("net10.0 هو ‎.NET الحديث", "net10.0 is modern .NET")]
    public void Parses_modern_dotnet()
    {
        var t = TfmParser.Parse("net10.0");
        Assert.Equal(FrameworkFamily.DotNet, t.Family);
        Assert.Equal(new Version(10, 0), t.Version);
        Assert.Null(t.Platform);
    }

    [Test("المنصة بعد الشرطة: net8.0-windows", "Platform after the dash: net8.0-windows")]
    public void Parses_platform()
    {
        var t = TfmParser.Parse(" NET8.0-Windows ");
        Assert.Equal(new TargetFramework(FrameworkFamily.DotNet, new Version(8, 0), "windows"), t);
    }

    [Test("net48 و net472 هما ‎.NET Framework", "net48 and net472 are .NET Framework")]
    public void Parses_framework()
    {
        Assert.Equal(new TargetFramework(FrameworkFamily.DotNetFramework, new Version(4, 8)), TfmParser.Parse("net48"));
        Assert.Equal(new TargetFramework(FrameworkFamily.DotNetFramework, new Version(4, 7, 2)), TfmParser.Parse("net472"));
    }

    [Test("netstandard و netcoreapp", "netstandard and netcoreapp")]
    public void Parses_standard_and_core()
    {
        Assert.Equal(new TargetFramework(FrameworkFamily.DotNetStandard, new Version(2, 0)), TfmParser.Parse("netstandard2.0"));
        Assert.Equal(new TargetFramework(FrameworkFamily.DotNetCore, new Version(3, 1)), TfmParser.Parse("netcoreapp3.1"));
    }

    [Test("النصوص غير الصالحة ترمي FormatException", "Invalid monikers throw FormatException")]
    public void Rejects_garbage()
    {
        Assert.Throws<FormatException>(() => TfmParser.Parse("java17"));
        Assert.Throws<FormatException>(() => TfmParser.Parse("netstandardX"));
        Assert.Throws<FormatException>(() => TfmParser.Parse("net4x"));
    }

    [Test("‎.NET الأحدث يستخدم مكتبات ‎.NET الأقدم وليس العكس", "Newer .NET uses older .NET libraries, not the reverse")]
    public void Modern_compat()
    {
        Assert.True(TfmParser.CanReference("net10.0", "net8.0"));
        Assert.False(TfmParser.CanReference("net8.0", "net10.0"));
    }

    [Test("مكتبة خاصة بـ Windows تحتاج مشروع Windows", "A Windows-only library needs a Windows project")]
    public void Platform_compat()
    {
        Assert.True(TfmParser.CanReference("net10.0-windows", "net8.0"));
        Assert.True(TfmParser.CanReference("net10.0-windows", "net8.0-windows"));
        Assert.False(TfmParser.CanReference("net10.0", "net8.0-windows"));
    }

    [Test("‎.NET Standard 2.0 يعمل على ‎.NET Framework 4.6.1+", ".NET Standard 2.0 works on .NET Framework 4.6.1+")]
    public void Standard20()
    {
        Assert.True(TfmParser.CanReference("net472", "netstandard2.0"));
        Assert.False(TfmParser.CanReference("net46", "netstandard2.0"));
        Assert.True(TfmParser.CanReference("net10.0", "netstandard2.0"));
        Assert.True(TfmParser.CanReference("netcoreapp2.1", "netstandard2.0"));
    }

    [Test("‎.NET Standard 2.1 لا يعمل على ‎.NET Framework إطلاقاً", ".NET Standard 2.1 never works on .NET Framework")]
    public void Standard21()
    {
        Assert.False(TfmParser.CanReference("net48", "netstandard2.1"));
        Assert.True(TfmParser.CanReference("net8.0", "netstandard2.1"));
        Assert.False(TfmParser.CanReference("netcoreapp2.2", "netstandard2.1"));
        Assert.False(TfmParser.CanReference("netstandard2.0", "netstandard2.1"));
    }

    [Test("‎.NET Framework لا يستخدم مكتبات ‎.NET الحديث", ".NET Framework cannot use modern .NET libraries")]
    public void Framework_cannot_use_modern() => Assert.False(TfmParser.CanReference("net48", "net8.0"));
}
