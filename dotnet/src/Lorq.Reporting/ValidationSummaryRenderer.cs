using Lorq.Core;

namespace Lorq.Reporting;

public static class ValidationSummaryRenderer
{
    public static object FromPackageResult(PackageValidationResult result)
    {
        return new
        {
            ok = result.Ok,
            package = result.Package is null ? null : new
            {
                package_id = result.Package.PackageId,
                package_kind = result.Package.PackageKind,
                schema_version = result.Package.PackageSchemaVersion,
                shards = result.Package.DeclaredShardIds,
                run_shard_count = result.Package.RunShards.Count,
                cell_count = result.Package.Cells.Count,
                expected_cell_count = result.Package.ExpectedCellIds.Count,
                missing_cell_count = result.Package.MissingCellIds.Count,
                judgement_count = result.Package.Judgements.Count,
                report_present = result.Package.Report is not null,
                integrity_ok = result.Package.IntegrityOk,
                integrity_warning_count = result.Package.IntegrityWarningCount,
            },
            diagnostics = result.Diagnostics,
        };
    }

    public static object FromMergeInputResult(MergeInputValidationResult result)
    {
        return new
        {
            ok = result.Ok,
            cell_count = result.CellIds.Count,
            duplicate_cell_ids = result.DuplicateCellIds,
            fingerprint_mismatch = result.FingerprintMismatch,
            diagnostics = result.Diagnostics,
        };
    }

    public static object FromIndexRebuildResult(LorqIndexRebuildResult result)
    {
        return new
        {
            ok = result.Ok,
            target_root = result.TargetRoot,
            generated_file_count = result.GeneratedFiles.Count,
            generated_files = result.GeneratedFiles,
            diagnostics = result.Diagnostics,
        };
    }

    public static object FromPackageMergeResult(LorqPackageMergeResult result)
    {
        return new
        {
            ok = result.Ok,
            package_root = result.PackageRoot,
            package_id = result.PackageId,
            shard_ids = result.ShardIds,
            cell_count = result.CellCount,
            expected_cell_count = result.ExpectedCellCount,
            missing_cell_ids = result.MissingCellIds,
            duplicate_cell_ids = result.DuplicateCellIds,
            fingerprint_mismatch = result.FingerprintMismatch,
            diagnostics = result.Diagnostics,
        };
    }

    public static object FromPackageJudgementResult(LorqPackageJudgementResult result)
    {
        return new
        {
            ok = result.Ok,
            package_root = result.PackageRoot,
            judgement_name = result.JudgementName,
            backend = result.Backend,
            cell_count = result.CellCount,
            judged_cell_count = result.JudgedCellCount,
            missing_fixture_cell_ids = result.MissingFixtureCellIds,
            missing_expected_cell_ids = result.MissingExpectedCellIds,
            score_summary = result.ScoreSummary,
            diagnostics = result.Diagnostics,
        };
    }

}
