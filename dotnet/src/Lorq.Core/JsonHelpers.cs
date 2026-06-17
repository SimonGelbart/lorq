using System.Text.Json;

namespace Lorq.Core;

internal static class JsonHelpers
{
    public static JsonDocument ReadDocument(string path)
    {
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    public static string RequiredString(JsonElement element, string property, string path)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new LorqPackageFormatException($"Missing or non-string property '{property}' in {path}.");
        }

        return value.GetString() ?? string.Empty;
    }

    public static int OptionalInt(JsonElement element, string property, int fallback = 0)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed) ? parsed : fallback;
    }

    public static bool OptionalBool(JsonElement element, string property, bool fallback = false)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => fallback,
        };
    }

    public static IReadOnlyList<string> OptionalStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToArray();
    }
}

public sealed class LorqPackageFormatException : Exception
{
    public LorqPackageFormatException(string message)
        : base(message)
    {
    }
}
