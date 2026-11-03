using System.Text.Json;

public static class ConfigFlattener
{
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
                foreach (var property in element.EnumerateObject())
                    Visit(property.Value, Join(prefix, property.Name), result);
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    Visit(item, Join(prefix, (index++).ToString()), result);
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;

            case JsonValueKind.Null:
                result[prefix] = null;
                break;

            default:   // numbers keep their JSON text ("5.50"); booleans become "True"/"False"
                result[prefix] = element.ToString();
                break;
        }
    }

    private static string Join(string prefix, string name) => prefix.Length == 0 ? name : prefix + ":" + name;

    public static Dictionary<string, string?> GetSection(Dictionary<string, string?> data, string section)
    {
        var start = section + ":";
        return data
            .Where(kv => kv.Key.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key[start.Length..], kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}
