namespace Lorq.Cli.Commands;

internal sealed class RunWorkspaceMaterializer
{
    public void Materialize(RunWorkspacePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        RecreateDirectory(plan.WorkspaceRoot);
        CopyDirectory(plan.RepositorySourceRoot, plan.WorkspaceRoot);
        foreach (var copy in plan.MaterializationCopies)
        {
            CopyMaterialization(copy, plan.WorkspaceRoot);
        }

        Directory.CreateDirectory(plan.EvidenceDirectory);
        Directory.CreateDirectory(plan.ArtifactsDirectory);
    }

    private static void CopyMaterialization(RunMaterializationCopy copy, string workspaceRoot)
    {
        var target = Path.GetFullPath(Path.Combine(workspaceRoot, copy.Target.Replace('/', Path.DirectorySeparatorChar)));
        EnsureTargetStaysInWorkspace(workspaceRoot, target);
        if (Directory.Exists(copy.Source))
        {
            CopyDirectory(copy.Source, target);
            return;
        }

        if (File.Exists(copy.Source))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(copy.Source, target, overwrite: true);
            return;
        }

        throw new FileNotFoundException("Mode materialization source does not exist.", copy.Source);
    }

    private static void EnsureTargetStaysInWorkspace(string workspaceRoot, string target)
    {
        var root = Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (target.StartsWith(root, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException($"Mode materialization target escapes workspace: {target}");
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static void CopyDirectory(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(source);
        }

        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var targetFile = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(file, targetFile, overwrite: true);
        }
    }
}
