using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lorq.Core;

namespace Lorq.Reporting;

internal static class PackageReportJson
{
    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonObject ReadObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new LorqPackageFormatException($"Expected JSON object in {path}.");
    }

    public static void Write(string path, JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(node);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonWriterOptions) + Environment.NewLine);
    }

    public static JsonObject CloneObject(JsonNode? node)
    {
        return node?.DeepClone() as JsonObject ?? new JsonObject();
    }

    public static JsonArray StringArray(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    public static IReadOnlyList<string> StringArrayValues(JsonNode? node)
    {
        return node is JsonArray array
            ? array.Select(item => item?.GetValue<string>() ?? string.Empty).Where(item => item.Length > 0).ToArray()
            : Array.Empty<string>();
    }

    public static int OptionalInt(JsonObject node, string property, int fallback)
    {
        return node[property]?.GetValue<int>() ?? fallback;
    }

    public static string StringProperty(JsonObject node, string property)
    {
        return node[property]?.GetValue<string>() ?? string.Empty;
    }
}
