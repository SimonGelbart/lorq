namespace Lorq.Core.PackageValidation;

internal sealed class PackageValidationDiagnostics
{
    private readonly List<LorqDiagnostic> items = new();

    public IReadOnlyList<LorqDiagnostic> Items => items;

    public bool HasErrors => items.Any(item => item.Severity == "error");

    public void Error(string code, string message, string? path = null)
    {
        items.Add(new LorqDiagnostic(code, "error", message, path));
    }

    public void Warning(string code, string message, string? path = null)
    {
        items.Add(new LorqDiagnostic(code, "warning", message, path));
    }
}
