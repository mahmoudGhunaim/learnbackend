using System.Text.Json;

public sealed class ConfigurationBuilderLite
{
    // Each provider contributes one layer. Order matters: later layers override earlier ones.
    private readonly List<Dictionary<string, string?>> _layers = new();

    public ConfigurationBuilderLite AddJson(string json)
    {
        _layers.Add(Flatten(json));
        return this;   // returning "this" enables the fluent style: builder.AddJson(...).AddCommandLine(...)
    }

    public ConfigurationBuilderLite AddEnvironmentVariables(IDictionary<string, string> env, string? prefix = null)
    {
        var layer = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in env)
        {
            if (prefix is not null && !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var key = (prefix is null ? name : name[prefix.Length..]).Replace("__", ":");
            layer[key] = value;
        }
        _layers.Add(layer);
        return this;
    }

    public ConfigurationBuilderLite AddCommandLine(string[] args)
    {
        var layer = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var hasPrefix = arg.StartsWith("--") || arg.StartsWith('/');
            var body = arg.TrimStart('-', '/');

            var parts = body.Split('=', 2);
            if (parts.Length == 2)
                layer[parts[0]] = parts[1];
            else if (hasPrefix && i + 1 < args.Length)
                layer[body] = args[++i];
        }
        _layers.Add(layer);
        return this;
    }

    public ConfigurationLite Build()
    {
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in _layers)
            foreach (var (key, value) in layer)
                merged[key] = value;   // last one wins
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
    private readonly Dictionary<string, string?> _data;

    public ConfigurationLite(IReadOnlyDictionary<string, string?> data)
    {
        _data = new Dictionary<string, string?>(data, StringComparer.OrdinalIgnoreCase);
    }

    public string? this[string key] => _data.GetValueOrDefault(key);

    public ConfigurationLite GetSection(string key)
    {
        var start = key + ":";
        return new ConfigurationLite(_data
            .Where(kv => kv.Key.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key[start.Length..], kv => kv.Value, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> GetChildKeys() =>
        _data.Keys
            .Select(k => k.Split(':')[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => int.TryParse(k, out _) ? 0 : 1)
            .ThenBy(k => int.TryParse(k, out var n) ? n : 0)
            .ThenBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public int GetInt(string key, int defaultValue) => int.TryParse(this[key], out var value) ? value : defaultValue;

    public bool GetBool(string key, bool defaultValue) => bool.TryParse(this[key], out var value) ? value : defaultValue;

    public string? GetConnectionString(string name) => this["ConnectionStrings:" + name];
}
