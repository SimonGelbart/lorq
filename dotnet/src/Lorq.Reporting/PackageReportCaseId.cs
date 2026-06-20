namespace Lorq.Reporting;

internal static class PackageReportCaseId
{
    public static string FromCellId(string cellId)
    {
        var separator = cellId.IndexOf("__", StringComparison.Ordinal);
        return separator < 0 ? cellId : cellId[..separator];
    }
}
