namespace Lorq.Core.PackageValidation;

internal sealed class ReportReferenceValidator
{
    private readonly PackageValidationDiagnostics diagnostics;

    public ReportReferenceValidator(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public ReportReference? Read(string root, PackageKind packageKind, ISet<string> knownJudgements)
    {
        var reportIndexPath = Path.Combine(root, ".lorq", "report.json");
        if (!File.Exists(reportIndexPath))
        {
            AddMissingReportWarning(packageKind, reportIndexPath);
            return null;
        }

        using var document = JsonHelpers.ReadDocument(reportIndexPath);
        var primaryJudgement = JsonHelpers.RequiredString(document.RootElement, "primary_judgement", reportIndexPath);
        var jsonPath = JsonHelpers.RequiredString(document.RootElement, "report", reportIndexPath);
        var markdownPath = JsonHelpers.RequiredString(document.RootElement, "markdown", reportIndexPath);
        var caseCount = JsonHelpers.OptionalInt(document.RootElement, "case_count", -1);

        ValidateReportReference(root, reportIndexPath, primaryJudgement, jsonPath, markdownPath, knownJudgements);
        return new ReportReference(primaryJudgement, jsonPath, markdownPath, caseCount);
    }

    private void AddMissingReportWarning(PackageKind packageKind, string reportIndexPath)
    {
        if (packageKind.IsMergedExperiment())
        {
            diagnostics.Warning("LORQ130", "Merged package does not have a report index yet.", reportIndexPath);
        }
    }

    private void ValidateReportReference(
        string root,
        string reportIndexPath,
        string primaryJudgement,
        string jsonPath,
        string markdownPath,
        ISet<string> knownJudgements)
    {
        if (!knownJudgements.Contains(primaryJudgement))
        {
            diagnostics.Error("LORQ131", $"Report primary judgement '{primaryJudgement}' is not present in judgements.", reportIndexPath);
        }

        if (!File.Exists(Path.Combine(root, jsonPath.Replace('/', Path.DirectorySeparatorChar))))
        {
            diagnostics.Error("LORQ132", $"Report JSON '{jsonPath}' is missing.", reportIndexPath);
        }

        if (!File.Exists(Path.Combine(root, markdownPath.Replace('/', Path.DirectorySeparatorChar))))
        {
            diagnostics.Error("LORQ133", $"Report Markdown '{markdownPath}' is missing.", reportIndexPath);
        }
    }
}
