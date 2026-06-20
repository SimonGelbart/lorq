using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed record PackageReportDocument(
    JsonObject Report,
    IReadOnlyList<JsonObject> CasePacks,
    IReadOnlyList<string> MissingExpectedCellIds);
