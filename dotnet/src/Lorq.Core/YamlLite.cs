namespace Lorq.Core;

internal static class YamlLite
{
    public static IReadOnlyDictionary<string, object> ParseTopLevel(string path)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        string? activeListKey = null;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (rawLine.StartsWith("  - ", StringComparison.Ordinal) && activeListKey is not null)
            {
                ((List<string>)result[activeListKey]).Add(rawLine.Trim()[2..].Trim());
                continue;
            }

            if (rawLine.StartsWith(' '))
            {
                continue;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                activeListKey = null;
                continue;
            }

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (value.Length == 0)
            {
                result[key] = new List<string>();
                activeListKey = key;
            }
            else if (int.TryParse(value, out var parsedInt))
            {
                result[key] = parsedInt;
                activeListKey = null;
            }
            else
            {
                result[key] = value.Trim('"', '\'');
                activeListKey = null;
            }
        }

        return result;
    }

    public static string RequiredString(IReadOnlyDictionary<string, object> values, string key, string path)
    {
        if (!values.TryGetValue(key, out var value) || value is not string text || string.IsNullOrWhiteSpace(text))
        {
            throw new LorqPackageFormatException($"Missing top-level YAML property '{key}' in {path}.");
        }

        return text;
    }

    public static int RequiredInt(IReadOnlyDictionary<string, object> values, string key, string path)
    {
        if (!values.TryGetValue(key, out var value) || value is not int parsed)
        {
            throw new LorqPackageFormatException($"Missing top-level integer YAML property '{key}' in {path}.");
        }

        return parsed;
    }

    public static IReadOnlyList<string> OptionalStringList(IReadOnlyDictionary<string, object> values, string key)
    {
        return values.TryGetValue(key, out var value) && value is List<string> list ? list : Array.Empty<string>();
    }
}
