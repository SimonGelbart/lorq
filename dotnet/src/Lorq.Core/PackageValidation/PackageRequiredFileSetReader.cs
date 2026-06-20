namespace Lorq.Core.PackageValidation;

internal sealed class PackageRequiredFileSetReader
{
    private readonly string root;
    private readonly PackageValidationDiagnostics diagnostics;

    public PackageRequiredFileSetReader(string root, PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(diagnostics);

        this.root = root;
        this.diagnostics = diagnostics;
    }

    public PackageRequiredFileSet? Read()
    {
        var experimentYaml = RequiredFile("experiment.yaml", "LORQ010");
        var coveragePath = RequiredFile(".lorq/coverage.json", "LORQ011");
        var fingerprintsPath = RequiredFile(".lorq/fingerprints.json", "LORQ012");
        var integrityPath = RequiredFile(".lorq/integrity.json", "LORQ013");
        var mergeLogPath = RequiredFile(".lorq/merge-log.json", "LORQ014");

        if (diagnostics.HasErrors)
        {
            return null;
        }

        return new PackageRequiredFileSet(
            experimentYaml!,
            coveragePath!,
            fingerprintsPath!,
            integrityPath!,
            mergeLogPath!);
    }

    private string? RequiredFile(string relativePath, string code)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            diagnostics.Error(code, $"Required package file '{relativePath}' is missing.", path);
            return null;
        }

        return path;
    }
}
