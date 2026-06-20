using System.Globalization;
using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal static class PackageReportFormatting
{
    public static decimal? Average(IReadOnlyList<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? null : Math.Round(values.Sum() / values.Count, 3, MidpointRounding.AwayFromZero);
    }

    public static JsonNode? NullableDecimal(decimal? value)
    {
        return value.HasValue ? JsonValue.Create(PythonFloatDecimal(value.Value)) : null;
    }

    public static string DecimalText(decimal? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = PythonFloatDecimal(value.Value).ToString(CultureInfo.InvariantCulture);
        return text.Contains('.', StringComparison.Ordinal) ? text : text + ".0";
    }

    public static string BoolText(bool value)
    {
        return value ? "True" : "False";
    }

    private static decimal PythonFloatDecimal(decimal value)
    {
        return value == decimal.Truncate(value)
            ? decimal.Parse(value.ToString("0", CultureInfo.InvariantCulture) + ".0", CultureInfo.InvariantCulture)
            : value;
    }
}
