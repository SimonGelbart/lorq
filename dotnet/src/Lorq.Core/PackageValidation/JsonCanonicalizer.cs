using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal static class JsonCanonicalizer
{
    public static string Canonicalize(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(element, writer);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(element, writer);
                break;
            case JsonValueKind.Array:
                WriteArray(element, writer);
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void WriteObject(JsonElement element, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            writer.WritePropertyName(property.Name);
            WriteCanonical(property.Value, writer);
        }
        writer.WriteEndObject();
    }

    private static void WriteArray(JsonElement element, Utf8JsonWriter writer)
    {
        writer.WriteStartArray();
        foreach (var item in element.EnumerateArray())
        {
            WriteCanonical(item, writer);
        }
        writer.WriteEndArray();
    }
}
