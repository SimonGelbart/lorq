using System.Text.Json;

namespace Lorq.Core;

public static class LorqPackageValidator
{
    public static PackageValidationResult Validate(string packageRoot)
    {
        var diagnostics = new List<LorqDiagnostic>();
        var root = Path.GetFullPath(packageRoot);

        try
        {
            if (!Directory.Exists(root))
            {
                AddError(diagnostics, "LORQ001", "Package root does not exist.", root);
                return new PackageValidationResult(false, null, diagnostics);
            }

            var experimentYaml = RequiredFile(root, "experiment.yaml", diagnostics, "LORQ010");
            var coveragePath = RequiredFile(root, ".lorq/coverage.json", diagnostics, "LORQ011");
            var fingerprintsPath = RequiredFile(root, ".lorq/fingerprints.json", diagnostics, "LORQ012");
            var integrityPath = RequiredFile(root, ".lorq/integrity.json", diagnostics, "LORQ013");
            var mergeLogPath = RequiredFile(root, ".lorq/merge-log.json", diagnostics, "LORQ014");

            if (diagnostics.Any(d => d.Severity == "error"))
            {
                return new PackageValidationResult(false, null, diagnostics);
            }

            var manifest = YamlLite.ParseTopLevel(experimentYaml!);
            var packageId = YamlLite.RequiredString(manifest, "package_id", experimentYaml!);
            var packageKind = YamlLite.RequiredString(manifest, "package_kind", experimentYaml!);
            var schemaVersion = YamlLite.RequiredInt(manifest, "package_schema_version", experimentYaml!);
            var declaredShards = YamlLite.OptionalStringList(manifest, "shards");
            if (schemaVersion != 1)
            {
                AddError(diagnostics, "LORQ020", $"Unsupported package_schema_version '{schemaVersion}'.", experimentYaml);
            }

            if (packageKind is not ("run_shard" or "merged_experiment"))
            {
                AddError(diagnostics, "LORQ021", $"Unsupported package_kind '{packageKind}'.", experimentYaml);
            }

            var coverage = ReadCoverage(root, coveragePath!, diagnostics);
            var fingerprints = ReadFingerprints(fingerprintsPath!, diagnostics);
            var integrity = ReadIntegrity(integrityPath!, diagnostics);
            ValidateMergeLog(mergeLogPath!, packageKind, diagnostics);

            var cells = ReadCells(root, diagnostics);
            ValidateCoverageAgainstCells(coverage, cells, diagnostics);
            ValidateFingerprintsAgainstCells(fingerprints, cells, diagnostics);

            var runShards = ReadRunShards(root, diagnostics);
            ValidateShardReferences(declaredShards, runShards, coverage.PresentCellIds, diagnostics);

            var judgements = ReadJudgements(root, cells.Select(c => c.CellId).ToHashSet(StringComparer.Ordinal), diagnostics);
            var report = ReadReportReference(root, packageKind, judgements.Select(j => j.Name).ToHashSet(StringComparer.Ordinal), diagnostics);

            var package = new ExperimentPackage(
                root,
                packageId,
                packageKind,
                schemaVersion,
                declaredShards,
                runShards,
                cells,
                coverage.ExpectedCellIds,
                coverage.MissingCellIds,
                judgements,
                report,
                integrity.WarningCount,
                integrity.Ok);

            return new PackageValidationResult(!diagnostics.Any(d => d.Severity == "error"), package, diagnostics);
        }
        catch (LorqPackageFormatException ex)
        {
            AddError(diagnostics, "LORQ900", ex.Message, root);
            return new PackageValidationResult(false, null, diagnostics);
        }
        catch (JsonException ex)
        {
            AddError(diagnostics, "LORQ901", $"Invalid JSON: {ex.Message}", root);
            return new PackageValidationResult(false, null, diagnostics);
        }
    }

    public static MergeInputValidationResult ValidateMergeInputs(IEnumerable<string> shardRoots)
    {
        var diagnostics = new List<LorqDiagnostic>();
        var allCells = new List<RunCell>();
        var fingerprints = new List<string>();

        foreach (var shardRoot in shardRoots)
        {
            var root = Path.GetFullPath(shardRoot);
            if (!Directory.Exists(root))
            {
                AddError(diagnostics, "LORQ001", "Shard root does not exist.", root);
                continue;
            }

            var shardCells = ReadCells(root, diagnostics);
            allCells.AddRange(shardCells);

            foreach (var cell in shardCells)
            {
                using var document = JsonHelpers.ReadDocument(Path.Combine(root, cell.EvidencePath));
                if (document.RootElement.TryGetProperty("fingerprint", out var fingerprint))
                {
                    fingerprints.Add(CanonicalizeJson(fingerprint));
                }
            }
        }

        var duplicateCellIds = allCells
            .GroupBy(cell => cell.CellId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var duplicate in duplicateCellIds)
        {
            AddError(diagnostics, "LORQ210", $"Duplicate cell id '{duplicate}' appears in multiple merge inputs.");
        }

        var fingerprintMismatch = fingerprints.Distinct(StringComparer.Ordinal).Count() > 1;
        if (fingerprintMismatch)
        {
            AddError(diagnostics, "LORQ220", "Merge inputs contain incompatible repository fingerprints.");
        }

        return new MergeInputValidationResult(
            !diagnostics.Any(d => d.Severity == "error"),
            diagnostics,
            allCells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray(),
            duplicateCellIds,
            fingerprintMismatch);
    }

    private static string? RequiredFile(string root, string relativePath, List<LorqDiagnostic> diagnostics, string code)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            AddError(diagnostics, code, $"Required package file '{relativePath}' is missing.", path);
            return null;
        }

        return path;
    }

    private static CoverageIndex ReadCoverage(string root, string path, List<LorqDiagnostic> diagnostics)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.coverage.", StringComparison.Ordinal))
        {
            AddError(diagnostics, "LORQ030", $"Unexpected coverage schema_version '{schema}'.", path);
        }

        var present = JsonHelpers.OptionalStringArray(document.RootElement, "present_cell_ids");
        var expected = JsonHelpers.OptionalStringArray(document.RootElement, "expected_cell_ids");
        var missing = JsonHelpers.OptionalStringArray(document.RootElement, "missing_cells");
        var declaredCellCount = JsonHelpers.OptionalInt(document.RootElement, "cell_count", -1);
        if (declaredCellCount != present.Count)
        {
            AddError(diagnostics, "LORQ031", $"coverage cell_count {declaredCellCount} does not match present_cell_ids count {present.Count}.", path);
        }

        foreach (var cellId in present.Except(expected, StringComparer.Ordinal))
        {
            AddError(diagnostics, "LORQ032", $"Present cell '{cellId}' is not listed in expected cells.", path);
        }

        var missingFromSet = expected.Except(present, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (!missing.SequenceEqual(missingFromSet, StringComparer.Ordinal))
        {
            AddError(diagnostics, "LORQ033", "coverage missing_cells does not match expected minus present cells.", path);
        }

        return new CoverageIndex(present, expected, missing);
    }

    private static IReadOnlyDictionary<string, string> ReadFingerprints(string path, List<LorqDiagnostic> diagnostics)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.fingerprints.", StringComparison.Ordinal))
        {
            AddError(diagnostics, "LORQ040", $"Unexpected fingerprint schema_version '{schema}'.", path);
        }

        if (!document.RootElement.TryGetProperty("by_cell", out var byCell) || byCell.ValueKind != JsonValueKind.Object)
        {
            AddError(diagnostics, "LORQ041", "fingerprints.json must contain object property by_cell.", path);
            return new Dictionary<string, string>();
        }

        return byCell.EnumerateObject().ToDictionary(
            property => property.Name,
            property => CanonicalizeJson(property.Value),
            StringComparer.Ordinal);
    }

    private static IntegrityIndex ReadIntegrity(string path, List<LorqDiagnostic> diagnostics)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.integrity.", StringComparison.Ordinal))
        {
            AddError(diagnostics, "LORQ050", $"Unexpected integrity schema_version '{schema}'.", path);
        }

        var ok = JsonHelpers.OptionalBool(document.RootElement, "ok", false);
        var warningCount = document.RootElement.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array
            ? warnings.GetArrayLength()
            : 0;
        return new IntegrityIndex(ok, warningCount);
    }

    private static void ValidateMergeLog(string path, string packageKind, List<LorqDiagnostic> diagnostics)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.merge-log.", StringComparison.Ordinal))
        {
            AddError(diagnostics, "LORQ060", $"Unexpected merge-log schema_version '{schema}'.", path);
        }

        var operation = JsonHelpers.RequiredString(document.RootElement, "operation", path);
        if (packageKind == "merged_experiment" && operation != "python-v0-merge-run-shards")
        {
            AddError(diagnostics, "LORQ061", $"Merged package has unexpected merge operation '{operation}'.", path);
        }
    }

    private static IReadOnlyList<RunCell> ReadCells(string root, List<LorqDiagnostic> diagnostics)
    {
        var cellIndexRoot = Path.Combine(root, ".lorq", "cells");
        if (!Directory.Exists(cellIndexRoot))
        {
            AddError(diagnostics, "LORQ070", "Package is missing .lorq/cells index directory.", cellIndexRoot);
            return Array.Empty<RunCell>();
        }

        var cells = new List<RunCell>();
        foreach (var cellPath in Directory.EnumerateFiles(cellIndexRoot, "*.json").Order(StringComparer.Ordinal))
        {
            using var document = JsonHelpers.ReadDocument(cellPath);
            var rootElement = document.RootElement;
            var cellId = JsonHelpers.RequiredString(rootElement, "cell_id", cellPath);
            var caseId = JsonHelpers.RequiredString(rootElement, "case_id", cellPath);
            var modeId = JsonHelpers.RequiredString(rootElement, "mode_id", cellPath);
            var attemptId = JsonHelpers.RequiredString(rootElement, "attempt_id", cellPath);
            var shardId = JsonHelpers.RequiredString(rootElement, "shard_id", cellPath);
            var status = JsonHelpers.RequiredString(rootElement, "status", cellPath);
            var finalAnswerPresent = rootElement.TryGetProperty("adapter_output", out var adapterOutput)
                && JsonHelpers.OptionalBool(adapterOutput, "final_answer_present", false);
            var expectedFileName = cellId + ".json";
            if (!Path.GetFileName(cellPath).Equals(expectedFileName, StringComparison.Ordinal))
            {
                AddError(diagnostics, "LORQ071", $"Cell index file name does not match cell_id '{cellId}'.", cellPath);
            }

            cells.Add(new RunCell(
                cellId,
                caseId,
                modeId,
                attemptId,
                shardId,
                status,
                finalAnswerPresent,
                Path.GetRelativePath(root, cellPath).Replace(Path.DirectorySeparatorChar, '/')));
        }

        return cells;
    }

    private static IReadOnlyList<RunShard> ReadRunShards(string root, List<LorqDiagnostic> diagnostics)
    {
        var runsRoot = Path.Combine(root, "runs");
        if (!Directory.Exists(runsRoot))
        {
            AddError(diagnostics, "LORQ080", "Package is missing runs directory.", runsRoot);
            return Array.Empty<RunShard>();
        }

        var shards = new List<RunShard>();
        foreach (var manifestPath in Directory.EnumerateFiles(runsRoot, "shard.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            using var document = JsonHelpers.ReadDocument(manifestPath);
            var shardId = JsonHelpers.RequiredString(document.RootElement, "shard_id", manifestPath);
            var cellIds = JsonHelpers.OptionalStringArray(document.RootElement, "cell_ids");
            var declaredCount = JsonHelpers.OptionalInt(document.RootElement, "cell_count", -1);
            if (declaredCount != cellIds.Count)
            {
                AddError(diagnostics, "LORQ081", $"Shard manifest cell_count {declaredCount} does not match cell_ids count {cellIds.Count}.", manifestPath);
            }

            shards.Add(new RunShard(shardId, declaredCount, cellIds));
        }

        return shards;
    }

    private static void ValidateCoverageAgainstCells(CoverageIndex coverage, IReadOnlyList<RunCell> cells, List<LorqDiagnostic> diagnostics)
    {
        var indexedCellIds = cells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray();
        var presentCellIds = coverage.PresentCellIds.Order(StringComparer.Ordinal).ToArray();
        if (!indexedCellIds.SequenceEqual(presentCellIds, StringComparer.Ordinal))
        {
            AddError(diagnostics, "LORQ090", ".lorq/cells index does not match coverage present_cell_ids.");
        }
    }

    private static void ValidateFingerprintsAgainstCells(IReadOnlyDictionary<string, string> fingerprints, IReadOnlyList<RunCell> cells, List<LorqDiagnostic> diagnostics)
    {
        var missing = cells.Select(cell => cell.CellId).Except(fingerprints.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        foreach (var cellId in missing)
        {
            AddError(diagnostics, "LORQ100", $"Cell '{cellId}' is missing from fingerprints by_cell.");
        }
    }

    private static void ValidateShardReferences(
        IReadOnlyList<string> declaredShards,
        IReadOnlyList<RunShard> runShards,
        IReadOnlyList<string> presentCells,
        List<LorqDiagnostic> diagnostics)
    {
        var manifestShardIds = runShards.Select(shard => shard.ShardId).Order(StringComparer.Ordinal).ToArray();
        var declaredShardIds = declaredShards.Order(StringComparer.Ordinal).ToArray();
        if (!declaredShardIds.SequenceEqual(manifestShardIds, StringComparer.Ordinal))
        {
            AddError(diagnostics, "LORQ110", "experiment.yaml shards do not match run shard manifests.");
        }

        var manifestCells = runShards.SelectMany(shard => shard.CellIds).Order(StringComparer.Ordinal).ToArray();
        var coverageCells = presentCells.Order(StringComparer.Ordinal).ToArray();
        if (!manifestCells.SequenceEqual(coverageCells, StringComparer.Ordinal))
        {
            AddError(diagnostics, "LORQ111", "Run shard manifest cells do not match coverage present_cell_ids.");
        }
    }

    private static IReadOnlyList<JudgementPass> ReadJudgements(string root, ISet<string> knownCellIds, List<LorqDiagnostic> diagnostics)
    {
        var judgementsRoot = Path.Combine(root, "judgements");
        if (!Directory.Exists(judgementsRoot))
        {
            return Array.Empty<JudgementPass>();
        }

        var passes = new List<JudgementPass>();
        foreach (var manifestPath in Directory.EnumerateFiles(judgementsRoot, "judgement.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            using var document = JsonHelpers.ReadDocument(manifestPath);
            var name = JsonHelpers.RequiredString(document.RootElement, "judgement_name", manifestPath);
            var backend = JsonHelpers.RequiredString(document.RootElement, "backend", manifestPath);
            var cellCount = JsonHelpers.OptionalInt(document.RootElement, "cell_count", -1);
            var judgedCellCount = JsonHelpers.OptionalInt(document.RootElement, "judged_cell_count", -1);
            var realLlmUsed = document.RootElement.TryGetProperty("source", out var source)
                && JsonHelpers.OptionalBool(source, "real_llm_used", false);
            var refs = document.RootElement.TryGetProperty("cell_judgement_refs", out var cellRefs) && cellRefs.ValueKind == JsonValueKind.Array
                ? cellRefs.EnumerateArray().ToArray()
                : Array.Empty<JsonElement>();

            if (cellCount != knownCellIds.Count)
            {
                AddError(diagnostics, "LORQ120", $"Judgement '{name}' cell_count does not match package cells.", manifestPath);
            }

            if (judgedCellCount != refs.Length)
            {
                AddError(diagnostics, "LORQ121", $"Judgement '{name}' judged_cell_count does not match cell_judgement_refs count.", manifestPath);
            }

            foreach (var cellRef in refs)
            {
                var cellId = JsonHelpers.RequiredString(cellRef, "cell_id", manifestPath);
                var relativePath = JsonHelpers.RequiredString(cellRef, "path", manifestPath);
                if (!knownCellIds.Contains(cellId))
                {
                    AddError(diagnostics, "LORQ122", $"Judgement '{name}' references unknown cell '{cellId}'.", manifestPath);
                }

                if (!File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))))
                {
                    AddError(diagnostics, "LORQ123", $"Judgement '{name}' references missing file '{relativePath}'.", manifestPath);
                }
            }

            passes.Add(new JudgementPass(name, backend, realLlmUsed, cellCount, judgedCellCount));
        }

        return passes;
    }

    private static ReportReference? ReadReportReference(string root, string packageKind, ISet<string> knownJudgements, List<LorqDiagnostic> diagnostics)
    {
        var reportIndexPath = Path.Combine(root, ".lorq", "report.json");
        if (!File.Exists(reportIndexPath))
        {
            if (packageKind == "merged_experiment")
            {
                AddWarning(diagnostics, "LORQ130", "Merged package does not have a report index yet.", reportIndexPath);
            }
            return null;
        }

        using var document = JsonHelpers.ReadDocument(reportIndexPath);
        var primaryJudgement = JsonHelpers.RequiredString(document.RootElement, "primary_judgement", reportIndexPath);
        var jsonPath = JsonHelpers.RequiredString(document.RootElement, "report", reportIndexPath);
        var markdownPath = JsonHelpers.RequiredString(document.RootElement, "markdown", reportIndexPath);
        var caseCount = JsonHelpers.OptionalInt(document.RootElement, "case_count", -1);

        if (!knownJudgements.Contains(primaryJudgement))
        {
            AddError(diagnostics, "LORQ131", $"Report primary judgement '{primaryJudgement}' is not present in judgements.", reportIndexPath);
        }

        if (!File.Exists(Path.Combine(root, jsonPath.Replace('/', Path.DirectorySeparatorChar))))
        {
            AddError(diagnostics, "LORQ132", $"Report JSON '{jsonPath}' is missing.", reportIndexPath);
        }

        if (!File.Exists(Path.Combine(root, markdownPath.Replace('/', Path.DirectorySeparatorChar))))
        {
            AddError(diagnostics, "LORQ133", $"Report Markdown '{markdownPath}' is missing.", reportIndexPath);
        }

        return new ReportReference(primaryJudgement, jsonPath, markdownPath, caseCount);
    }

    private static string CanonicalizeJson(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(element, writer);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(item, writer);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void AddError(List<LorqDiagnostic> diagnostics, string code, string message, string? path = null)
    {
        diagnostics.Add(new LorqDiagnostic(code, "error", message, path));
    }

    private static void AddWarning(List<LorqDiagnostic> diagnostics, string code, string message, string? path = null)
    {
        diagnostics.Add(new LorqDiagnostic(code, "warning", message, path));
    }

    private sealed record CoverageIndex(
        IReadOnlyList<string> PresentCellIds,
        IReadOnlyList<string> ExpectedCellIds,
        IReadOnlyList<string> MissingCellIds);

    private sealed record IntegrityIndex(bool Ok, int WarningCount);
}
