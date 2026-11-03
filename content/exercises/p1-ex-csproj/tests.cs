using LearnHarness;

public class Tests
{
    private const string WebApi = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
            <PackageReference Include="Serilog.AspNetCore">
              <Version>9.0.0</Version>
            </PackageReference>
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\Shop.Core\Shop.Core.csproj" />
            <ProjectReference Include="..\Shop.Infrastructure\Shop.Infrastructure.csproj" />
          </ItemGroup>
        </Project>
        """;

    private const string Library = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFrameworks> netstandard2.0; net8.0 ;net10.0; </TargetFrameworks>
            <ImplicitUsings>True</ImplicitUsings>
          </PropertyGroup>
        </Project>
        """;

    [Test("قراءة الـ Sdk و TargetFramework", "Reads Sdk and TargetFramework")]
    public void Reads_basics()
    {
        var info = CsprojReader.Read(WebApi);
        Assert.Equal("Microsoft.NET.Sdk.Web", info.Sdk);
        Assert.SequenceEqual(new[] { "net10.0" }, info.TargetFrameworks);
    }

    [Test("TargetFrameworks المتعددة مع تنظيف الفراغات", "Multiple TargetFrameworks, trimmed")]
    public void Reads_multi_targeting() =>
        Assert.SequenceEqual(new[] { "netstandard2.0", "net8.0", "net10.0" }, CsprojReader.Read(Library).TargetFrameworks);

    [Test("OutputType الافتراضي Library", "OutputType defaults to Library")]
    public void Default_output_type()
    {
        Assert.Equal("Library", CsprojReader.Read(Library).OutputType);
        Assert.Equal("Exe", CsprojReader.Read("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup></Project>").OutputType);
    }

    [Test("Nullable و ImplicitUsings", "Nullable and ImplicitUsings")]
    public void Reads_flags()
    {
        var web = CsprojReader.Read(WebApi);
        Assert.True(web.NullableEnabled);
        Assert.True(web.ImplicitUsings);
        var lib = CsprojReader.Read(Library);
        Assert.False(lib.NullableEnabled);
        Assert.True(lib.ImplicitUsings, "\"True\" should count as enabled (case-insensitive)");
    }

    [Test("الحزم مع الإصدار كخاصية أو كعنصر", "Packages with Version as attribute or element")]
    public void Reads_packages() =>
        Assert.SequenceEqual(
            new[] { new PackageRef("Microsoft.EntityFrameworkCore.Sqlite", "10.0.0"), new PackageRef("Serilog.AspNetCore", "9.0.0") },
            CsprojReader.Read(WebApi).Packages);

    [Test("أسماء المشاريع المرجعية من مسارات بـ \\", "Project reference names from backslash paths")]
    public void Reads_project_references() =>
        Assert.SequenceEqual(new[] { "Shop.Core", "Shop.Infrastructure" }, CsprojReader.Read(WebApi).ProjectReferences);

    [Test("IsWebProject", "IsWebProject")]
    public void Detects_web()
    {
        Assert.True(CsprojReader.IsWebProject(CsprojReader.Read(WebApi)));
        Assert.False(CsprojReader.IsWebProject(CsprojReader.Read(Library)));
        Assert.False(CsprojReader.IsWebProject(CsprojReader.Read("<Project Sdk=\"Microsoft.NET.Sdk.WebAssembly\" />")));
    }
}
