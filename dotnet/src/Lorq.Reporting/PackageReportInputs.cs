using System.Text.Json.Nodes;
using Lorq.Core;

namespace Lorq.Reporting;

internal sealed record PackageReportInputs(
    ExperimentPackage Package,
    IReadOnlyList<JsonObject> Cells,
    JsonObject Coverage,
    JsonObject Integrity,
    JsonObject Fingerprints,
    JsonObject JudgementManifest,
    IReadOnlyDictionary<string, JsonObject> Judgements,
    string PrimaryJudgement);
