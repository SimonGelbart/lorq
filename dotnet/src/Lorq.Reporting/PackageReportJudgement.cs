using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal static class PackageReportJudgement
{
    public static decimal? Score(JsonObject? judgement)
    {
        return judgement?["quality"]?["overall_score"]?.GetValue<decimal>();
    }
}
