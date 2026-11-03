using LearnHarness;

public class Tests
{
    private const string AppSettings = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "AllowedHosts": "*",
          "Cors": { "Origins": ["http://localhost:5173", "https://shop.example"] },
          "Sandbox": { "TimeoutSeconds": 5, "Ratio": 0.50, "Enabled": true, "Proxy": null }
        }
        """;

    [Test("المفاتيح المتداخلة بالنقطتين", "Nested keys joined with colons")]
    public void Nested_keys()
    {
        var data = ConfigFlattener.Flatten(AppSettings);
        Assert.Equal("Information", data["Logging:LogLevel:Default"]);
        Assert.Equal("Warning", data["Logging:LogLevel:Microsoft.AspNetCore"]);
        Assert.Equal("*", data["AllowedHosts"]);
    }

    [Test("المصفوفات تستخدم الفهرس كمفتاح", "Arrays use the index as key")]
    public void Arrays()
    {
        var data = ConfigFlattener.Flatten(AppSettings);
        Assert.Equal("http://localhost:5173", data["Cors:Origins:0"]);
        Assert.Equal("https://shop.example", data["Cors:Origins:1"]);
        Assert.False(data.ContainsKey("Cors:Origins"), "The array itself is not a value");
    }

    [Test("الأرقام كنصها، والمنطقية True/False، و null كـ null (مثل ASP.NET Core)", "Numbers keep their text, bools are True/False, null is null (like ASP.NET Core)")]
    public void Scalars()
    {
        var data = ConfigFlattener.Flatten(AppSettings);
        Assert.Equal("5", data["Sandbox:TimeoutSeconds"]);
        Assert.Equal("0.50", data["Sandbox:Ratio"]);
        Assert.Equal("True", data["Sandbox:Enabled"]);
        Assert.True(data.ContainsKey("Sandbox:Proxy"));
        Assert.Null(data["Sandbox:Proxy"]);
    }

    [Test("عدد المفاتيح الكلي صحيح", "Total key count is right")]
    public void Count() => Assert.Equal(9, ConfigFlattener.Flatten(AppSettings).Count);

    [Test("المفاتيح لا تهتم بحالة الأحرف", "Keys are case-insensitive")]
    public void Case_insensitive()
    {
        var data = ConfigFlattener.Flatten(AppSettings);
        Assert.Equal("Information", data["logging:loglevel:default"]);
    }

    [Test("GetSection يحذف البادئة ويحترم حدود الأقسام", "GetSection strips the prefix and respects section boundaries")]
    public void Sections()
    {
        var data = ConfigFlattener.Flatten("""{ "Log": { "Path": "/tmp" }, "Logging": { "Level": "Debug" } }""");
        var log = ConfigFlattener.GetSection(data, "Log");
        Assert.Equal(1, log.Count);
        Assert.Equal("/tmp", log["path"]);

        var nested = ConfigFlattener.GetSection(ConfigFlattener.Flatten(AppSettings), "logging:LogLevel");
        Assert.Equal(2, nested.Count);
        Assert.Equal("Information", nested["Default"]);
    }
}
