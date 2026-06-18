namespace Lorq.Cli.Commands;

internal static class RunModeMaterialization
{
    public static IReadOnlyList<RunMaterializationCopy> ReadCopies(string suiteRoot, string modePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(modePath);
        var copies = new List<RunMaterializationCopy>();
        string? pendingSource = null;

        foreach (var rawLine in File.ReadLines(modePath))
        {
            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("- from:", StringComparison.Ordinal))
            {
                pendingSource = ResolveSource(suiteRoot, trimmed.Split(':', 2)[1].Trim());
                continue;
            }

            if (pendingSource is not null && trimmed.StartsWith("to:", StringComparison.Ordinal))
            {
                copies.Add(new RunMaterializationCopy(pendingSource, trimmed.Split(':', 2)[1].Trim().Trim('\'', '"')));
                pendingSource = null;
            }
        }

        return copies;
    }

    private static string ResolveSource(string suiteRoot, string source)
    {
        var trimmed = source.Trim().Trim('\'', '"');
        return Path.IsPathRooted(trimmed) ? trimmed : Path.GetFullPath(Path.Combine(suiteRoot, trimmed));
    }
}
