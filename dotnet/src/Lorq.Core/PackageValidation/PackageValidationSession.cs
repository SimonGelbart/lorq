using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal sealed class PackageValidationSession
{
    private readonly string packageRoot;
    private readonly PackageValidationDiagnostics diagnostics = new();
    private readonly CoverageIndexValidator coverage;
    private readonly FingerprintIndexValidator fingerprints;
    private readonly IntegrityIndexReader integrity;
    private readonly MergeLogValidator mergeLog;
    private readonly RunCellIndexReader cells;
    private readonly RunShardManifestReader shards;
    private readonly JudgementIndexValidator judgements;
    private readonly ReportReferenceValidator reports;
    private readonly PackageManifestReader manifests;

    public PackageValidationSession(string packageRoot)
    {
        ArgumentNullException.ThrowIfNull(packageRoot);

        this.packageRoot = packageRoot;
        coverage = new CoverageIndexValidator(diagnostics);
        fingerprints = new FingerprintIndexValidator(diagnostics);
        integrity = new IntegrityIndexReader(diagnostics);
        mergeLog = new MergeLogValidator(diagnostics);
        cells = new RunCellIndexReader(diagnostics);
        shards = new RunShardManifestReader(diagnostics);
        judgements = new JudgementIndexValidator(diagnostics);
        reports = new ReportReferenceValidator(diagnostics);
        manifests = new PackageManifestReader(diagnostics);
    }

    public PackageValidationResult Validate()
    {
        var root = Path.GetFullPath(packageRoot);
        try
        {
            return ValidateRoot(root);
        }
        catch (LorqPackageFormatException exception)
        {
            diagnostics.Error("LORQ900", exception.Message, root);
            return FailedResult();
        }
        catch (JsonException exception)
        {
            diagnostics.Error("LORQ901", $"Invalid JSON: {exception.Message}", root);
            return FailedResult();
        }
    }

    private PackageValidationResult ValidateRoot(string root)
    {
        if (!Directory.Exists(root))
        {
            diagnostics.Error("LORQ001", "Package root does not exist.", root);
            return FailedResult();
        }

        var files = new PackageRequiredFileSetReader(root, diagnostics).Read();
        if (files is null)
        {
            return FailedResult();
        }

        return ValidateFiles(root, files);
    }

    private PackageValidationResult ValidateFiles(string root, PackageRequiredFileSet files)
    {
        var manifest = manifests.Read(files.ExperimentYaml);
        var coverageIndex = coverage.Read(files.CoveragePath);
        var fingerprintIndex = fingerprints.Read(files.FingerprintsPath);
        var integrityIndex = integrity.Read(files.IntegrityPath);
        mergeLog.Validate(files.MergeLogPath, manifest.PackageKind);

        var runCells = cells.Read(root);
        coverage.ValidateCells(coverageIndex, runCells);
        fingerprints.ValidateCells(fingerprintIndex, runCells);

        var runShards = shards.Read(root);
        shards.ValidateShardReferences(manifest.DeclaredShards, runShards, coverageIndex.PresentCellIds);

        var judgementPasses = ReadJudgements(root, runCells);
        var report = reports.Read(root, manifest.PackageKind, judgementPasses.Select(judgement => judgement.Name).ToHashSet(StringComparer.Ordinal));

        return BuildResult(root, manifest, coverageIndex, integrityIndex, runCells, runShards, judgementPasses, report);
    }

    private IReadOnlyList<JudgementPass> ReadJudgements(string root, IReadOnlyList<RunCell> runCells)
    {
        var knownCellIds = runCells.Select(cell => cell.CellId).ToHashSet(StringComparer.Ordinal);
        return judgements.Read(root, knownCellIds);
    }

    private PackageValidationResult BuildResult(
        string root,
        PackageManifest manifest,
        PackageCoverageIndex coverageIndex,
        PackageIntegrityIndex integrityIndex,
        IReadOnlyList<RunCell> runCells,
        IReadOnlyList<RunShard> runShards,
        IReadOnlyList<JudgementPass> judgementPasses,
        ReportReference? report)
    {
        var package = new ExperimentPackage(
            root,
            manifest.PackageId.Value,
            manifest.PackageKind.Value,
            manifest.SchemaVersion.Value,
            manifest.DeclaredShards.Select(shardId => shardId.Value).ToArray(),
            runShards,
            runCells,
            coverageIndex.ExpectedCellIds.Select(cellId => cellId.Value).ToArray(),
            coverageIndex.MissingCellIds.Select(cellId => cellId.Value).ToArray(),
            judgementPasses,
            report,
            integrityIndex.WarningCount,
            integrityIndex.Ok);

        return new PackageValidationResult(!diagnostics.HasErrors, package, diagnostics.Items);
    }

    private PackageValidationResult FailedResult()
    {
        return new PackageValidationResult(false, null, diagnostics.Items);
    }
}
