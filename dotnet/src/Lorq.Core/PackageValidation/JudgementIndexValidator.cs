using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal sealed class JudgementIndexValidator
{
    private readonly PackageValidationDiagnostics diagnostics;

    public JudgementIndexValidator(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public IReadOnlyList<JudgementPass> Read(string root, ISet<string> knownCellIds)
    {
        var judgementsRoot = Path.Combine(root, "judgements");
        if (!Directory.Exists(judgementsRoot))
        {
            return Array.Empty<JudgementPass>();
        }

        var passes = new List<JudgementPass>();
        foreach (var manifestPath in Directory.EnumerateFiles(judgementsRoot, "judgement.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            passes.Add(ReadJudgement(root, manifestPath, knownCellIds));
        }

        return passes;
    }

    private JudgementPass ReadJudgement(string root, string manifestPath, ISet<string> knownCellIds)
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

        ValidateCounts(name, manifestPath, knownCellIds, cellCount, judgedCellCount, refs);
        ValidateReferences(root, name, manifestPath, knownCellIds, refs);
        return new JudgementPass(name, backend, realLlmUsed, cellCount, judgedCellCount);
    }

    private void ValidateCounts(
        string name,
        string manifestPath,
        ISet<string> knownCellIds,
        int cellCount,
        int judgedCellCount,
        IReadOnlyCollection<JsonElement> refs)
    {
        if (cellCount != knownCellIds.Count)
        {
            diagnostics.Error("LORQ120", $"Judgement '{name}' cell_count does not match package cells.", manifestPath);
        }

        if (judgedCellCount != refs.Count)
        {
            diagnostics.Error("LORQ121", $"Judgement '{name}' judged_cell_count does not match cell_judgement_refs count.", manifestPath);
        }
    }

    private void ValidateReferences(
        string root,
        string name,
        string manifestPath,
        ISet<string> knownCellIds,
        IReadOnlyCollection<JsonElement> refs)
    {
        foreach (var cellRef in refs)
        {
            ValidateReference(root, name, manifestPath, knownCellIds, cellRef);
        }
    }

    private void ValidateReference(
        string root,
        string name,
        string manifestPath,
        ISet<string> knownCellIds,
        JsonElement cellRef)
    {
        var cellId = JsonHelpers.RequiredString(cellRef, "cell_id", manifestPath);
        var relativePath = JsonHelpers.RequiredString(cellRef, "path", manifestPath);
        if (!knownCellIds.Contains(cellId))
        {
            diagnostics.Error("LORQ122", $"Judgement '{name}' references unknown cell '{cellId}'.", manifestPath);
        }

        if (!File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))))
        {
            diagnostics.Error("LORQ123", $"Judgement '{name}' references missing file '{relativePath}'.", manifestPath);
        }
    }
}
