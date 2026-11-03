using System.Text.Json;

public sealed class ConfigurationBuilderLite
{
    public ConfigurationBuilderLite AddJson(string json) => throw new NotImplementedException();

    public ConfigurationBuilderLite AddEnvironmentVariables(IDictionary<string, string> env, string? prefix = null)
        => throw new NotImplementedException();

    public ConfigurationBuilderLite AddCommandLine(string[] args) => throw new NotImplementedException();

    public ConfigurationLite Build() => throw new NotImplementedException();

    // جاهزة من التمرين السابق: تحوّل JSON إلى قاموس مسطّح بمفاتيح "A:B:C"
    public static Dictionary<string, string?> Flatten(string json)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        Visit(doc.RootElement, "", result);
        return result;
    }

    private static void Visit(JsonElement element, string prefix, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in element.EnumerateObject()) Visit(p.Value, Join(prefix, p.Name), result);
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray()) Visit(item, Join(prefix, (i++).ToString()), result);
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;
            case JsonValueKind.Null:
                result[prefix] = null;
                break;
            default:
                result[prefix] = element.ToString();
                break;
        }
    }

    private static string Join(string prefix, string name) => prefix.Length == 0 ? name : prefix + ":" + name;
}

public sealed class ConfigurationLite
{
    public ConfigurationLite(IReadOnlyDictionary<string, string?> data)
    {
        throw new NotImplementedException();
    }

    public string? this[string key] => throw new NotImplementedException();

    public ConfigurationLite GetSection(string key) => throw new NotImplementedException();

    public IReadOnlyList<string> GetChildKeys() => throw new NotImplementedException();

    public int GetInt(string key, int defaultValue) => throw new NotImplementedException();

    public bool GetBool(string key, bool defaultValue) => throw new NotImplementedException();

    public string? GetConnectionString(string name) => throw new NotImplementedException();
}
