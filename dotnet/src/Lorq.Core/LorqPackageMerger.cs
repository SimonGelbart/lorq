using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lorq.Core;

/// <summary>
/// Writes merged v1-alpha experiment packages from deterministic run shards.
/// </summary>
public static class LorqPackageMerger
{
    private const string ContractVersion = "lorq.contract.v1alpha1";

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LorqPackageMergeResult Merge(LorqPackageMergeRequest request)
    {
        var diagnostics = new List<LorqDiagnostic>();
        var outputRoot = Path.GetFullPath(request.OutputRoot);

        try
        {
            var normalizedShardRoots = request.ShardRoots.Select(Path.GetFullPath).ToArray();
            var mergeValidation = LorqPackageValidator.ValidateMergeInputs(normalizedShardRoots);
            diagnostics.AddRange(mergeValidation.Diagnostics);
            if (request.Strict && !mergeValidation.Ok)
            {
                return FailedResult(request, outputRoot, Array.Empty<ExperimentPackage>(), mergeValidation, diagnostics);
            }

            var shardPackages = ReadShardPackages(normalizedShardRoots, diagnostics);
            if (request.Strict && diagnostics.Any(diagnostic => diagnostic.Severity == "error"))
            {
                return FailedResult(request, outputRoot, shardPackages, mergeValidation, diagnostics);
            }

            WriteMergedPackage(request, outputRoot, shardPackages, mergeValidation);
            var expectedCellIds = ExpectedCellIds(request, mergeValidation.CellIds);
            var sourceWarnings = SourceShardWarnings(shardPackages);
            var rebuild = LorqPackageIndexRebuilder.Rebuild(
                outputRoot,
                outputRoot,
                new LorqIndexRebuildOptions(expectedCellIds, sourceWarnings));

            diagnostics.AddRange(rebuild.Diagnostics);
            return SuccessfulResult(request, outputRoot, shardPackages, mergeValidation, diagnostics);
        }
        catch (LorqPackageFormatException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ900", "error", exception.Message, outputRoot));
            return FailedResult(request, outputRoot, Array.Empty<ExperimentPackage>(), EmptyMergeValidation(), diagnostics);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ901", "error", $"Invalid JSON: {exception.Message}", outputRoot));
            return FailedResult(request, outputRoot, Array.Empty<ExperimentPackage>(), EmptyMergeValidation(), diagnostics);
        }
    }

    public static LorqPackageMergeResult Merge(
        IReadOnlyList<string> shardRoots,
        string outputRoot,
        string packageId,
        string? benchmarkPath = null,
        bool strict = true)
    {
        return Merge(new LorqPackageMergeRequest(shardRoots, outputRoot, packageId, benchmarkPath, strict));
    }

    private static IReadOnlyList<ExperimentPackage> ReadShardPackages(IReadOnlyList<string> shardRoots, List<LorqDiagnostic> diagnostics)
    {
        var packages = new List<ExperimentPackage>();
        foreach (var shardRoot in shardRoots)
        {
            AddShardPackage(packages, shardRoot, diagnostics);
        }

        return packages;
    }

    private static void AddShardPackage(List<ExperimentPackage> packages, string shardRoot, List<LorqDiagnostic> diagnostics)
    {
        var validation = LorqPackageValidator.Validate(shardRoot);
        diagnostics.AddRange(validation.Diagnostics);
        if (validation.Package is null)
        {
            return;
        }

        if (validation.Package.PackageKind != "run_shard")
        {
            diagnostics.Add(new LorqDiagnostic("LORQ230", "error", "Merge input is not a run_shard package.", shardRoot));
            return;
        }

        packages.Add(validation.Package);
    }

    private static void WriteMergedPackage(
        LorqPackageMergeRequest request,
        string outputRoot,
        IReadOnlyList<ExperimentPackage> shardPackages,
        MergeInputValidationResult mergeValidation)
    {
        RecreateDirectory(outputRoot);
        Directory.CreateDirectory(Path.Combine(outputRoot, "runs"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "judgements"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "reports", "cases"));
        Directory.CreateDirectory(Path.Combine(outputRoot, ".lorq"));

        foreach (var package in shardPackages)
        {
            CopyRunShard(package, outputRoot);
        }

        var shardIds = shardPackages.SelectMany(package => package.DeclaredShardIds).Order(StringComparer.Ordinal).ToArray();
        WriteExperimentYaml(outputRoot, request.PackageId, shardIds, mergeValidation.CellIds.Distinct(StringComparer.Ordinal).Count());
        WriteJson(Path.Combine(outputRoot, ".lorq", "merge-log.json"), MergeLog(request, outputRoot, shardPackages, mergeValidation));
    }

    private static void CopyRunShard(ExperimentPackage package, string outputRoot)
    {
        foreach (var shard in package.RunShards)
        {
            var source = Path.Combine(package.RootPath, "runs", shard.ShardId);
            var destination = Path.Combine(outputRoot, "runs", shard.ShardId);
            CopyDirectory(source, destination);
        }
    }

    private static JsonObject MergeLog(
        LorqPackageMergeRequest request,
        string outputRoot,
        IReadOnlyList<ExperimentPackage> shardPackages,
        MergeInputValidationResult mergeValidation)
    {
        var inputRows = new JsonArray();
        foreach (var package in shardPackages)
        {
            inputRows.Add(MergeInputRow(package, outputRoot));
        }

        var expectedCellIds = ExpectedCellIds(request, mergeValidation.CellIds);
        var presentCellIds = mergeValidation.CellIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var missingCellCount = expectedCellIds.Count(cellId => !presentCellIds.Contains(cellId));

        return new JsonObject
        {
            ["schema_version"] = "lorq.merge-log.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["operation"] = "python-v0-merge-run-shards",
            ["inputs"] = inputRows,
            ["outputs"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "lorq-merged-experiment",
                    ["package_id"] = request.PackageId,
                    ["path"] = ".",
                },
            },
            ["strict"] = request.Strict,
            ["cell_count"] = mergeValidation.CellIds.Distinct(StringComparer.Ordinal).Count(),
            ["expected_cell_count"] = expectedCellIds.Count,
            ["missing_cell_count"] = missingCellCount,
            ["duplicate_cell_count"] = mergeValidation.DuplicateCellIds.Count,
            ["fingerprint_mismatch"] = mergeValidation.FingerprintMismatch,
        };
    }

    private static JsonObject MergeInputRow(ExperimentPackage package, string outputRoot)
    {
        var shard = package.RunShards.Single();
        return new JsonObject
        {
            ["kind"] = "lorq-run-shard",
            ["path"] = PortablePath(package.RootPath, outputRoot),
            ["shard_id"] = shard.ShardId,
            ["cell_count"] = shard.CellCount,
        };
    }

    private static IReadOnlyList<JsonObject> SourceShardWarnings(IReadOnlyList<ExperimentPackage> packages)
    {
        var warnings = new List<JsonObject>();
        foreach (var package in packages)
        {
            warnings.AddRange(SourceShardWarnings(package));
        }

        return warnings;
    }

    private static IEnumerable<JsonObject> SourceShardWarnings(ExperimentPackage package)
    {
        var shardId = package.RunShards.Single().ShardId;
        var path = Path.Combine(package.RootPath, ".lorq", "integrity.json");
        if (!File.Exists(path))
        {
            yield break;
        }

        var integrity = ReadJsonObject(path);
        if (integrity["warnings"] is not JsonArray warnings)
        {
            yield break;
        }

        foreach (var warning in warnings.OfType<JsonObject>())
        {
            yield return SourceShardWarning(shardId, warning);
        }
    }

    private static JsonObject SourceShardWarning(string shardId, JsonObject warning)
    {
        var sourced = new JsonObject
        {
            ["source_shard"] = shardId,
        };
        foreach (var property in warning)
        {
            sourced[property.Key] = property.Value?.DeepClone();
        }

        return sourced;
    }

    private static IReadOnlyList<string> ExpectedCellIds(LorqPackageMergeRequest request, IReadOnlyList<string> fallbackCellIds)
    {
        if (!string.IsNullOrWhiteSpace(request.BenchmarkPath))
        {
            return LorqBenchmarkExpectedCells.ReadFrom(request.BenchmarkPath);
        }

        return fallbackCellIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private static LorqPackageMergeResult SuccessfulResult(
        LorqPackageMergeRequest request,
        string outputRoot,
        IReadOnlyList<ExperimentPackage> shardPackages,
        MergeInputValidationResult mergeValidation,
        IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        var expectedCellIds = ExpectedCellIds(request, mergeValidation.CellIds);
        var presentCellIds = mergeValidation.CellIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        return new LorqPackageMergeResult(
            !diagnostics.Any(diagnostic => diagnostic.Severity == "error"),
            outputRoot,
            request.PackageId,
            shardPackages.SelectMany(package => package.DeclaredShardIds).Order(StringComparer.Ordinal).ToArray(),
            mergeValidation.CellIds.Distinct(StringComparer.Ordinal).Count(),
            expectedCellIds.Count,
            expectedCellIds.Where(cellId => !presentCellIds.Contains(cellId)).ToArray(),
            mergeValidation.DuplicateCellIds,
            mergeValidation.FingerprintMismatch,
            diagnostics);
    }

    private static LorqPackageMergeResult FailedResult(
        LorqPackageMergeRequest request,
        string outputRoot,
        IReadOnlyList<ExperimentPackage> shardPackages,
        MergeInputValidationResult mergeValidation,
        IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        return new LorqPackageMergeResult(
            false,
            outputRoot,
            request.PackageId,
            shardPackages.SelectMany(package => package.DeclaredShardIds).Order(StringComparer.Ordinal).ToArray(),
            mergeValidation.CellIds.Distinct(StringComparer.Ordinal).Count(),
            0,
            Array.Empty<string>(),
            mergeValidation.DuplicateCellIds,
            mergeValidation.FingerprintMismatch,
            diagnostics);
    }

    private static MergeInputValidationResult EmptyMergeValidation()
    {
        return new MergeInputValidationResult(false, Array.Empty<LorqDiagnostic>(), Array.Empty<string>(), Array.Empty<string>(), false);
    }

    private static void WriteExperimentYaml(string outputRoot, string packageId, IReadOnlyList<string> shardIds, int cellCount)
    {
        var renderedShards = string.Join(Environment.NewLine, shardIds.Select(shardId => $"  - {shardId}"));
        var text = $"""
package_schema_version: 1
package_kind: merged_experiment
package_id: {packageId}
created_by:
  name: agent-eval python-v0
  implementation: python
  version: 1.2.4
shards:
{renderedShards}
cell_count: {cellCount}
""";
        File.WriteAllText(Path.Combine(outputRoot, "experiment.yaml"), text + Environment.NewLine);
    }

    private static string PortablePath(string path, string outputRoot)
    {
        var resolved = Path.GetFullPath(path);
        var outputParent = Directory.GetParent(Path.GetFullPath(outputRoot))?.FullName;
        if (outputParent is not null && IsSameDirectory(Path.GetDirectoryName(resolved), outputParent))
        {
            return Path.GetFileName(resolved);
        }

        return Path.GetRelativePath(Path.GetFullPath(outputRoot), resolved).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static bool IsSameDirectory(string? left, string right)
    {
        return left is not null && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.Ordinal);
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new LorqPackageFormatException($"Missing run shard payload: {source}.");
        }

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private static JsonObject ReadJsonObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new LorqPackageFormatException($"Expected JSON object in {path}.");
    }

    private static void WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonWriterOptions) + Environment.NewLine);
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }
}
