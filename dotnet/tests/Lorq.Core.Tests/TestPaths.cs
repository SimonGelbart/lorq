namespace Lorq.Core.Tests;

internal static class TestPaths
{
    public static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current is not null)
        {
            if (IsRepoRoot(current))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not find repo root from test output directory.");
    }

    private static bool IsRepoRoot(string path)
    {
        return File.Exists(Path.Combine(path, "eval.config.yaml"))
            && Directory.Exists(Path.Combine(path, "fixtures"));
    }
}
