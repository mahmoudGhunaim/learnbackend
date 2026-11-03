// Realistic mistakes: keys are case-sensitive (default Dictionary comparer), arrays are stored
// as one raw JSON string, and GetSection matches "Log" inside "Logging" (no ":" boundary).
using System.Text.Json;

public static class ConfigFlattener
{
    public static Dictionary<string, string?> Flatten(string json)
    {
        var result = new Dictionary<string, string?>();
        using var doc = JsonDocument.Parse(json);
        Visit(doc.RootElement, "", result);
        return result;
    }

    private static void Visit(JsonElement element, string prefix, Dictionary<string, string?> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in element.EnumerateObject())
                Visit(p.Value, prefix == "" ? p.Name : prefix + ":" + p.Name, result);
        }
        else
        {
            result[prefix] = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        }
    }

    public static Dictionary<string, string?> GetSection(Dictionary<string, string?> data, string section) =>
        data.Where(kv => kv.Key.StartsWith(section))
            .ToDictionary(kv => kv.Key.Substring(section.Length).TrimStart(':'), kv => kv.Value);
}
