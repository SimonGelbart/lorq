using Lorq.Core;

namespace Lorq.Cli.Commands;

internal static class RunSuiteRepositoryCatalog
{
    public static string ReadPath(string suiteRoot, string repositoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        var configPath = Path.Combine(suiteRoot, "eval.config.yaml");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Suite eval.config.yaml does not exist.", configPath);
        }

        return ReadPathFromLines(File.ReadLines(configPath), repositoryId, configPath);
    }

    private static string ReadPathFromLines(IEnumerable<string> lines, string repositoryId, string configPath)
    {
        var insideRepository = false;
        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith($"  {repositoryId}:", StringComparison.Ordinal))
            {
                insideRepository = true;
                continue;
            }

            if (insideRepository && rawLine.StartsWith("  ", StringComparison.Ordinal) && !rawLine.StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            if (insideRepository && rawLine.TrimStart().StartsWith("path:", StringComparison.Ordinal))
            {
                return rawLine.Trim().Split(':', 2)[1].Trim().Trim('\'', '"');
            }
        }

        throw new LorqPackageFormatException($"Repository '{repositoryId}' in '{configPath}' does not declare a local path.");
    }
}
