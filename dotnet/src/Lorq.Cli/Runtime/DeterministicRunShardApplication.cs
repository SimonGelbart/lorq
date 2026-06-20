using Lorq.Core;
using Lorq.Cli.Commands.Parsing;

namespace Lorq.Cli.Runtime;

internal sealed class DeterministicRunShardApplication
{
    private readonly RunAdapterFactory adapterFactory;
    private readonly RunWorkspacePlanner workspacePlanner;
    private readonly RunWorkspaceMaterializer workspaceMaterializer;
    private readonly RunCellExecutor cellExecutor;
    private readonly RunShardResultWriter resultWriter;

    public DeterministicRunShardApplication(
        RunAdapterFactory adapterFactory,
        RunWorkspacePlanner workspacePlanner,
        RunWorkspaceMaterializer workspaceMaterializer,
        RunCellExecutor cellExecutor,
        RunShardResultWriter resultWriter)
    {
        ArgumentNullException.ThrowIfNull(adapterFactory);
        ArgumentNullException.ThrowIfNull(workspacePlanner);
        ArgumentNullException.ThrowIfNull(workspaceMaterializer);
        ArgumentNullException.ThrowIfNull(cellExecutor);
        ArgumentNullException.ThrowIfNull(resultWriter);
        this.adapterFactory = adapterFactory;
        this.workspacePlanner = workspacePlanner;
        this.workspaceMaterializer = workspaceMaterializer;
        this.cellExecutor = cellExecutor;
        this.resultWriter = resultWriter;
    }

    public async ValueTask<LorqRunShardWriteResult> RunAsync(RunOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var suiteRoot = Path.GetFullPath(options.SuiteRoot);
        var adapter = adapterFactory.Create(options, suiteRoot);
        var plan = DeterministicBenchmarkShardPlan.ReadFrom(ResolveFromSuite(suiteRoot, options.BenchmarkPath), options.ShardId);
        var cells = new List<LorqRunShardCellEvidence>();

        foreach (var cell in plan.Cells)
        {
            var workspace = workspacePlanner.Plan(new RunWorkspacePlanningRequest(suiteRoot, options.OutputRoot, options.ShardId, cell, options.WorkRoot));
            workspaceMaterializer.Materialize(workspace);
            cells.Add(await cellExecutor.RunAsync(options.ShardId, adapter, cell, workspace, cancellationToken));
        }

        return resultWriter.Write(options, cells);
    }

    private static string ResolveFromSuite(string suiteRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(suiteRoot, path);
    }
}
