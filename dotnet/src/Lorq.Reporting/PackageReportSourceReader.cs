using System.Text.Json.Nodes;
using Lorq.Core;

namespace Lorq.Reporting;

internal sealed class PackageReportSourceReader
{
    public PackageReportInputs Read(string packageRoot, ExperimentPackage package, string primaryJudgement)
    {
        ArgumentNullException.ThrowIfNull(packageRoot);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(primaryJudgement);

        return new PackageReportInputs(
            package,
            LoadPackageCells(packageRoot),
            PackageReportJson.ReadObject(Path.Combine(packageRoot, ".lorq", "coverage.json")),
            PackageReportJson.ReadObject(Path.Combine(packageRoot, ".lorq", "integrity.json")),
            PackageReportJson.ReadObject(Path.Combine(packageRoot, ".lorq", "fingerprints.json")),
            LoadPrimaryJudgementManifest(packageRoot, primaryJudgement),
            LoadPrimaryJudgements(packageRoot, primaryJudgement),
            primaryJudgement);
    }

    private static JsonObject LoadPrimaryJudgementManifest(string packageRoot, string primaryJudgement)
    {
        var indexPath = Path.Combine(packageRoot, ".lorq", "judgements", primaryJudgement + ".json");
        if (File.Exists(indexPath))
        {
            return PackageReportJson.ReadObject(indexPath);
        }

        var manifestPath = Path.Combine(packageRoot, "judgements", primaryJudgement, "judgement.manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new LorqPackageFormatException($"Missing primary LORQ judgement pass: {primaryJudgement}.");
        }

        return PackageReportJson.ReadObject(manifestPath);
    }

    private static IReadOnlyDictionary<string, JsonObject> LoadPrimaryJudgements(string packageRoot, string primaryJudgement)
    {
        var cellsRoot = Path.Combine(packageRoot, "judgements", primaryJudgement, "cells");
        if (!Directory.Exists(cellsRoot))
        {
            throw new LorqPackageFormatException($"Missing judgement cells directory: {cellsRoot}.");
        }

        return Directory.EnumerateFiles(cellsRoot, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(PackageReportJson.ReadObject)
            .Where(judgement => !string.IsNullOrWhiteSpace(PackageReportJson.StringProperty(judgement, "cell_id")))
            .ToDictionary(judgement => PackageReportJson.StringProperty(judgement, "cell_id"), StringComparer.Ordinal);
    }

    private static IReadOnlyList<JsonObject> LoadPackageCells(string packageRoot)
    {
        var cellsRoot = Path.Combine(packageRoot, ".lorq", "cells");
        if (!Directory.Exists(cellsRoot))
        {
            throw new LorqPackageFormatException($"Missing LORQ cell index: {cellsRoot}.");
        }

        return Directory.EnumerateFiles(cellsRoot, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(PackageReportJson.ReadObject)
            .ToArray();
    }
}
