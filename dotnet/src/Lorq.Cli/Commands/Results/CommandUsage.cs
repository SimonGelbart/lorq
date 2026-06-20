namespace Lorq.Cli.Commands.Results;

public static class CommandUsage
{
    public const string Text = "Usage: lorq adapter-conformance --adapter-command <executable> [--adapter-arg <arg>] --out <output-root> [--adapter-working-directory <dir>] [--timeout-ms <milliseconds>] | validate-package <package-root> | validate-merge-inputs <shard-root> <shard-root> [...] | rebuild-indexes <package-root> <target-root> | merge-shards <shard-root> <shard-root> [...] --out <package-root> --package-id <id> [--benchmark <path>] [--allow-incompatible] | judge-package <package-root> --name <judge-name> --fixture <path> [--allow-missing-fixtures] | report-package <package-root> [--primary-judgement <judge-name>]";
}
