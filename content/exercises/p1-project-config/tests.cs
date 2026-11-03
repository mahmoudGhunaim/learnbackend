using LearnHarness;

public class Tests
{
    private const string AppSettings = """
        {
          "ConnectionStrings": { "Default": "Data Source=shop.db" },
          "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
          "Features": { "Checkout": true, "Reviews": false },
          "Sandbox": { "TimeoutSeconds": 5 },
          "Cors": { "Origins": [ "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" ] }
        }
        """;

    private const string AppSettingsDevelopment = """
        {
          "Logging": { "LogLevel": { "Default": "Debug" } },
          "Features": { "Reviews": true }
        }
        """;

    [Test("القراءة من JSON بدون اعتبار لحالة الأحرف", "Reads from JSON, case-insensitive")]
    public void Reads_json()
    {
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).Build();
        Assert.Equal("Information", config["Logging:LogLevel:Default"]);
        Assert.Equal("Information", config["LOGGING:loglevel:DEFAULT"]);
        Assert.Null(config["Nope:Missing"]);
    }

    [Test("ملف البيئة يستبدل الإعدادات الأساسية (الأخير يفوز)", "Environment file overrides the base file (last wins)")]
    public void Environment_json_overrides()
    {
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).AddJson(AppSettingsDevelopment).Build();
        Assert.Equal("Debug", config["Logging:LogLevel:Default"]);
        Assert.Equal("Warning", config["Logging:LogLevel:Microsoft.AspNetCore"], "Keys not in the second file must survive");
        Assert.True(config.GetBool("Features:Reviews", false));
    }

    [Test("متغيرات البيئة: __ تصبح : وتستبدل JSON", "Environment variables: __ becomes : and overrides JSON")]
    public void Environment_variables()
    {
        var env = new Dictionary<string, string>
        {
            ["Logging__LogLevel__Default"] = "Error",
            ["ConnectionStrings__Default"] = "Server=prod;Database=shop",
        };
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).AddEnvironmentVariables(env).Build();
        Assert.Equal("Error", config["Logging:LogLevel:Default"]);
        Assert.Equal("Server=prod;Database=shop", config.GetConnectionString("Default"));
    }

    [Test("بادئة متغيرات البيئة تُصفّى وتُحذف", "The environment prefix filters and is stripped")]
    public void Environment_prefix()
    {
        var env = new Dictionary<string, string>
        {
            ["SHOP_Sandbox__TimeoutSeconds"] = "30",
            ["shop_Features__Checkout"] = "false",
            ["PATH"] = "C:\\Windows",
        };
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).AddEnvironmentVariables(env, "SHOP_").Build();
        Assert.Equal(30, config.GetInt("Sandbox:TimeoutSeconds", 0));
        Assert.False(config.GetBool("Features:Checkout", true));
        Assert.Null(config["PATH"]);
        Assert.Null(config["SHOP_Sandbox:TimeoutSeconds"]);
    }

    [Test("سطر الأوامر بكل صيغه، وهو الأعلى أولوية", "Command line in all its forms, highest priority")]
    public void Command_line()
    {
        var env = new Dictionary<string, string> { ["Sandbox__TimeoutSeconds"] = "30" };
        var config = new ConfigurationBuilderLite()
            .AddJson(AppSettings)
            .AddEnvironmentVariables(env)
            .AddCommandLine(new[] { "--Sandbox:TimeoutSeconds=60", "--Logging:LogLevel:Default", "Trace", "/Features:Reviews=true", "Mode=Demo" })
            .Build();
        Assert.Equal(60, config.GetInt("Sandbox:TimeoutSeconds", 0));
        Assert.Equal("Trace", config["Logging:LogLevel:Default"]);
        Assert.True(config.GetBool("Features:Reviews", false));
        Assert.Equal("Demo", config["Mode"]);
    }

    [Test("GetSection يعطي إعدادات فرعية", "GetSection returns a sub-configuration")]
    public void Sections()
    {
        var logging = new ConfigurationBuilderLite().AddJson(AppSettings).Build().GetSection("logging");
        Assert.Equal("Information", logging["LogLevel:Default"]);
        Assert.Equal("Warning", logging.GetSection("LogLevel")["Microsoft.AspNetCore"]);
    }

    [Test("GetChildKeys: الأرقام رقمياً ثم الأسماء أبجدياً", "GetChildKeys: numbers numerically, then names")]
    public void Child_keys()
    {
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).Build();
        Assert.SequenceEqual(new[] { "ConnectionStrings", "Cors", "Features", "Logging", "Sandbox" }, config.GetChildKeys());
        var origins = config.GetSection("Cors:Origins").GetChildKeys();
        Assert.Equal(11, origins.Count);
        Assert.SequenceEqual(new[] { "0", "1", "2" }, origins.Take(3));
        Assert.Equal("10", origins[10]);
    }

    [Test("القيم الافتراضية للمفاتيح المفقودة أو غير الصالحة", "Defaults for missing or invalid values")]
    public void Defaults()
    {
        var config = new ConfigurationBuilderLite().AddJson(AppSettings).AddCommandLine(new[] { "--Sandbox:Retries=many" }).Build();
        Assert.Equal(3, config.GetInt("Sandbox:Retries", 3));
        Assert.Equal(7, config.GetInt("Nope", 7));
        Assert.True(config.GetBool("Nope", true));
        Assert.Equal(5, config.GetInt("Sandbox:TimeoutSeconds", 0));
    }
}
