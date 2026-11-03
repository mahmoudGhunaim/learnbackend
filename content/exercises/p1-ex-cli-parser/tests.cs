using LearnHarness;

public class Tests
{
    [Test("dotnet new webapi -n Shop --framework net10.0", "dotnet new webapi -n Shop --framework net10.0")]
    public void Parses_new_command()
    {
        var c = DotnetCli.Parse(new[] { "new", "webapi", "-n", "Shop", "--framework", "net10.0" });
        Assert.Equal("new", c.Verb);
        Assert.SequenceEqual(new[] { "webapi" }, c.Arguments);
        Assert.Equal("Shop", c.Options["name"]);
        Assert.Equal("net10.0", c.Options["framework"]);
        Assert.Equal(2, c.Options.Count);
    }

    [Test("الصيغة --key=value", "The --key=value form")]
    public void Parses_equals_form()
    {
        var c = DotnetCli.Parse(new[] { "build", "--configuration=Release" });
        Assert.Equal("Release", c.Options["configuration"]);
    }

    [Test("الأعلام بلا قيمة = null", "Flags without a value are null")]
    public void Parses_flags()
    {
        var c = DotnetCli.Parse(new[] { "build", "--no-restore", "-c", "Release", "--nologo" });
        Assert.True(c.Options.ContainsKey("no-restore"));
        Assert.Null(c.Options["no-restore"]);
        Assert.Null(c.Options["nologo"]);
        Assert.Equal("Release", c.Options["configuration"]);
    }

    [Test("الاختصارات -o و -v", "Aliases -o and -v")]
    public void Parses_aliases()
    {
        var c = DotnetCli.Parse(new[] { "publish", "-o", "./out", "-v", "minimal" });
        Assert.Equal("./out", c.Options["output"]);
        Assert.Equal("minimal", c.Options["verbosity"]);
    }

    [Test("الأمر والمفاتيح بأحرف صغيرة", "Verb and keys are lowercased")]
    public void Lowercases()
    {
        var c = DotnetCli.Parse(new[] { "ADD", "package", "Microsoft.EntityFrameworkCore.Sqlite", "--Version", "10.0.0" });
        Assert.Equal("add", c.Verb);
        Assert.SequenceEqual(new[] { "package", "Microsoft.EntityFrameworkCore.Sqlite" }, c.Arguments);
        Assert.Equal("10.0.0", c.Options["version"]);
    }

    [Test("مصفوفة فارغة ترمي ArgumentException", "Empty args throw ArgumentException")]
    public void Empty_args() => Assert.Throws<ArgumentException>(() => DotnetCli.Parse(Array.Empty<string>()));

    [Test("أي الأوامر تحتاج مشروعاً موجوداً", "Which commands need an existing project")]
    public void Requires_project()
    {
        Assert.True(DotnetCli.RequiresProject(DotnetCli.Parse(new[] { "run" })));
        Assert.True(DotnetCli.RequiresProject(DotnetCli.Parse(new[] { "test", "--no-build" })));
        Assert.False(DotnetCli.RequiresProject(DotnetCli.Parse(new[] { "new", "console" })));
        Assert.False(DotnetCli.RequiresProject(DotnetCli.Parse(new[] { "--info" })));
    }
}
