namespace Lorq.Cli.Commands;

public static class CommandUsage
{
    public const string Text = "Usage: lorq validate-package <package-root> | validate-merge-inputs <shard-root> <shard-root> [...] | rebuild-indexes <package-root> <target-root> | merge-shards <shard-root> <shard-root> [...] --out <package-root> --package-id <id> [--benchmark <path>] [--allow-incompatible] | judge-package <package-root> --name <judge-name> --fixture <path> [--allow-missing-fixtures] | report-package <package-root> [--primary-judgement <judge-name>]";
}
