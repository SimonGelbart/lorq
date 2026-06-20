using Lorq.Core;

namespace Lorq.Cli.Runtime;

internal static class RunCaseMetadata
{
    public static string ReadRepositoryId(string casePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePath);
        foreach (var line in File.ReadLines(casePath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("repo:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = trimmed.Split(':', 2)[1].Trim().Trim('\'', '"');
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new LorqPackageFormatException($"Case file '{casePath}' does not declare a repo.");
    }
}
