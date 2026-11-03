// Realistic mistakes: the FIRST layer wins (TryAdd), the env-var prefix is not stripped,
// "--Key Value" (with a space) is not supported, and child keys sort "10" before "2".
using System.Text.Json;

public sealed class ConfigurationBuilderLite
{
    private readonly List<Dictionary<string, string?>> _layers = new();

    public ConfigurationBuilderLite AddJson(string json) { _layers.Add(Flatten(json)); return this; }

    public ConfigurationBuilderLite AddEnvironmentVariables(IDictionary<string, string> env, string? prefix = null)
    {
        var layer = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in env)
            if (prefix is null || name.StartsWith(prefix)) layer[name.Replace("__", ":")] = value;
        _layers.Add(layer);
        return this;
    }

    public ConfigurationBuilderLite AddCommandLine(string[] args)
    {
        var layer = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in args)
        {
            var parts = arg.TrimStart('-', '/').Split('=', 2);
            if (parts.Length == 2) layer[parts[0]] = parts[1];
        }
        _layers.Add(layer);
        return this;
    }

    public ConfigurationLite Build()
    {
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in _layers)
            foreach (var (key, value) in layer)
                merged.TryAdd(key, value);
        return new ConfigurationLite(merged);
    }

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
                foreach (var p in element.EnumerateObject()) Visit(p.Value, prefix.Length == 0 ? p.Name : prefix + ":" + p.Name, result);
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray()) Visit(item, prefix + ":" + (i++), result);
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
}

public sealed class ConfigurationLite(IReadOnlyDictionary<string, string?> data)
{
    public string? this[string key] => data.GetValueOrDefault(key);

    public ConfigurationLite GetSection(string key) =>
        new(data.Where(kv => kv.Key.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key[(key.Length + 1)..], kv => kv.Value, StringComparer.OrdinalIgnoreCase));

    public IReadOnlyList<string> GetChildKeys() => data.Keys.Select(k => k.Split(':')[0]).Distinct().Order().ToList();

    public int GetInt(string key, int defaultValue) => int.TryParse(this[key], out var v) ? v : defaultValue;
    public bool GetBool(string key, bool defaultValue) => bool.TryParse(this[key], out var v) ? v : defaultValue;
    public string? GetConnectionString(string name) => this["ConnectionStrings:" + name];
}
