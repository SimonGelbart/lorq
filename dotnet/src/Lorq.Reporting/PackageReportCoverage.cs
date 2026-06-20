using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal static class PackageReportCoverage
{
    public static IReadOnlyList<string> MissingExpectedCellIds(JsonObject coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        return PackageReportJson.StringArrayValues(coverage["missing_cells"]);
    }

    public static int WarningCount(JsonObject integrity)
    {
        ArgumentNullException.ThrowIfNull(integrity);
        return integrity["warnings"] is JsonArray warnings ? warnings.Count : 0;
    }
}
