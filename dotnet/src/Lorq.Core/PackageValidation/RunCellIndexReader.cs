namespace Lorq.Core.PackageValidation;

internal sealed class RunCellIndexReader
{
    private readonly PackageValidationDiagnostics diagnostics;

    public RunCellIndexReader(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public IReadOnlyList<RunCell> Read(string root)
    {
        var cellIndexRoot = Path.Combine(root, ".lorq", "cells");
        if (!Directory.Exists(cellIndexRoot))
        {
            diagnostics.Error("LORQ070", "Package is missing .lorq/cells index directory.", cellIndexRoot);
            return Array.Empty<RunCell>();
        }

        var cells = new List<RunCell>();
        foreach (var cellPath in Directory.EnumerateFiles(cellIndexRoot, "*.json").Order(StringComparer.Ordinal))
        {
            cells.Add(ReadCell(root, cellPath));
        }

        return cells;
    }

    private RunCell ReadCell(string root, string cellPath)
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
        ValidateFileName(cellId, cellPath);

        return new RunCell(
            cellId,
            caseId,
            modeId,
            attemptId,
            shardId,
            status,
            finalAnswerPresent,
            Path.GetRelativePath(root, cellPath).Replace(Path.DirectorySeparatorChar, '/'));
    }

    private void ValidateFileName(string cellId, string cellPath)
    {
        var expectedFileName = cellId + ".json";
        if (!Path.GetFileName(cellPath).Equals(expectedFileName, StringComparison.Ordinal))
        {
            diagnostics.Error("LORQ071", $"Cell index file name does not match cell_id '{cellId}'.", cellPath);
        }
    }
}
