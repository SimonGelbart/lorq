using Lorq.Core;

namespace Lorq.Cli.Commands;

internal sealed class RunWorkspacePlanner
{
    public RunWorkspacePlan Plan(RunWorkspacePlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var suiteRoot = Path.GetFullPath(request.SuiteRoot);
        var outputRoot = Path.GetFullPath(request.OutputRoot);
        var casePath = Path.Combine(suiteRoot, "cases", request.Cell.CaseId + ".yaml");
        var modePath = Path.Combine(suiteRoot, "modes", request.Cell.ModeId + ".yaml");
        var cellId = CellId(request.Cell);
        var attemptId = AttemptId(request.Cell);
        var workspaceRoot = WorkspaceRoot(request, outputRoot, cellId);
        var evidenceDirectory = Path.Combine(outputRoot, ".lorq", "tmp", cellId);
        var artifactsDirectory = Path.Combine(evidenceDirectory, "artifacts");

        return new RunWorkspacePlan(
            cellId,
            attemptId,
            request.Cell.CaseId,
            request.Cell.ModeId,
            casePath,
            modePath,
            RepositorySourceRoot(suiteRoot, casePath),
            workspaceRoot,
            evidenceDirectory,
            artifactsDirectory,
            RunModeMaterialization.ReadCopies(suiteRoot, modePath));
    }

    private static string CellId(DeterministicBenchmarkCell cell)
    {
        return $"{cell.CaseId}__{cell.ModeId}__{AttemptId(cell)}";
    }

    private static string AttemptId(DeterministicBenchmarkCell cell)
    {
        return $"attempt-{cell.Attempt:000}";
    }

    private static string WorkspaceRoot(RunWorkspacePlanningRequest request, string outputRoot, string cellId)
    {
        var workRoot = request.WorkRoot;
        if (string.IsNullOrWhiteSpace(workRoot))
        {
            return Path.Combine(outputRoot + ".workspaces", cellId);
        }

        var resolved = Path.GetFullPath(workRoot);
        return Path.Combine(resolved, request.ShardId, cellId);
    }

    private static string RepositorySourceRoot(string suiteRoot, string casePath)
    {
        var repositoryId = RunCaseMetadata.ReadRepositoryId(casePath);
        var configuredPath = RunSuiteRepositoryCatalog.ReadPath(suiteRoot, repositoryId);
        return Path.GetFullPath(Path.Combine(suiteRoot, configuredPath));
    }
}
