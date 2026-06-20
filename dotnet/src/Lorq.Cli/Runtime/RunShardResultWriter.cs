using Lorq.Core;
using Lorq.Cli.Commands.Parsing;

namespace Lorq.Cli.Runtime;

internal sealed class RunShardResultWriter
{
    public LorqRunShardWriteResult Write(RunOptions options, IReadOnlyList<LorqRunShardCellEvidence> cells)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cells);
        return LorqRunShardPackageWriter.Write(new LorqRunShardWriteRequest(options.PackageId, options.ShardId, options.OutputRoot, cells));
    }
}
