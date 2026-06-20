using System.Text.Json;
using Lorq.Core;

namespace Lorq.Reporting;

internal sealed class PackageReportRenderingPipeline
{
    private readonly PackageReportSourceReader sourceReader = new();
    private readonly PackageReportDocumentBuilder documentBuilder = new();
    private readonly PackageReportFileWriter fileWriter = new();
    private readonly PackageReportResultFactory resultFactory = new();

    public LorqPackageReportResult Render(LorqPackageReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var packageRoot = Path.GetFullPath(request.PackageRoot);
        var diagnostics = new List<LorqDiagnostic>();

        try
        {
            var validation = LorqPackageValidator.Validate(packageRoot);
            diagnostics.AddRange(validation.Diagnostics);
            if (validation.Package is null)
            {
                return resultFactory.FailedResult(request, packageRoot, diagnostics);
            }

            var inputs = sourceReader.Read(packageRoot, validation.Package, request.PrimaryJudgement);
            if (inputs.Cells.Count == 0)
            {
                diagnostics.Add(new LorqDiagnostic("LORQ410", "error", "Package has no LORQ cells to report.", packageRoot));
                return resultFactory.FailedResult(request, packageRoot, diagnostics);
            }

            var document = documentBuilder.Build(inputs);
            fileWriter.Write(packageRoot, document, request.PrimaryJudgement);
            return resultFactory.SuccessfulResult(request, packageRoot, document, diagnostics);
        }
        catch (LorqPackageFormatException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ900", "error", exception.Message, packageRoot));
            return resultFactory.FailedResult(request, packageRoot, diagnostics);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ901", "error", $"Invalid JSON: {exception.Message}", packageRoot));
            return resultFactory.FailedResult(request, packageRoot, diagnostics);
        }
    }
}
