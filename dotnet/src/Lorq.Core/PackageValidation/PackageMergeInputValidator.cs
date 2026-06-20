namespace Lorq.Core.PackageValidation;

internal sealed class PackageMergeInputValidator
{
    private readonly IEnumerable<string> shardRoots;
    private readonly PackageValidationDiagnostics diagnostics = new();
    private readonly RunCellIndexReader cells;

    public PackageMergeInputValidator(IEnumerable<string> shardRoots)
    {
        ArgumentNullException.ThrowIfNull(shardRoots);

        this.shardRoots = shardRoots;
        cells = new RunCellIndexReader(diagnostics);
    }

    public MergeInputValidationResult Validate()
    {
        var allCells = new List<RunCell>();
        var fingerprints = new List<string>();

        foreach (var shardRoot in shardRoots)
        {
            ReadShardInput(shardRoot, allCells, fingerprints);
        }

        var duplicateCellIds = DuplicateCellIds(allCells);
        AddDuplicateDiagnostics(duplicateCellIds);
        var fingerprintMismatch = FingerprintMismatch(fingerprints);
        AddFingerprintMismatchDiagnostic(fingerprintMismatch);

        return new MergeInputValidationResult(
            !diagnostics.HasErrors,
            diagnostics.Items,
            allCells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray(),
            duplicateCellIds,
            fingerprintMismatch);
    }

    private void ReadShardInput(string shardRoot, List<RunCell> allCells, List<string> fingerprints)
    {
        var root = Path.GetFullPath(shardRoot);
        if (!Directory.Exists(root))
        {
            diagnostics.Error("LORQ001", "Shard root does not exist.", root);
            return;
        }

        var shardCells = cells.Read(root);
        allCells.AddRange(shardCells);
        foreach (var cell in shardCells)
        {
            AddFingerprint(root, cell, fingerprints);
        }
    }

    private static void AddFingerprint(string root, RunCell cell, List<string> fingerprints)
    {
        using var document = JsonHelpers.ReadDocument(Path.Combine(root, cell.EvidencePath));
        if (document.RootElement.TryGetProperty("fingerprint", out var fingerprint))
        {
            fingerprints.Add(JsonCanonicalizer.Canonicalize(fingerprint));
        }
    }

    private static IReadOnlyList<string> DuplicateCellIds(IReadOnlyList<RunCell> allCells)
    {
        return allCells
            .GroupBy(cell => cell.CellId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool FingerprintMismatch(IEnumerable<string> fingerprints)
    {
        return fingerprints.Distinct(StringComparer.Ordinal).Count() > 1;
    }

    private void AddDuplicateDiagnostics(IEnumerable<string> duplicateCellIds)
    {
        foreach (var duplicate in duplicateCellIds)
        {
            diagnostics.Error("LORQ210", $"Duplicate cell id '{duplicate}' appears in multiple merge inputs.");
        }
    }

    private void AddFingerprintMismatchDiagnostic(bool fingerprintMismatch)
    {
        if (fingerprintMismatch)
        {
            diagnostics.Error("LORQ220", "Merge inputs contain incompatible repository fingerprints.");
        }
    }
}
