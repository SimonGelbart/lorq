namespace Lorq.Cli.Commands;

public sealed class LorqCommandCatalog
{
    private readonly IReadOnlyDictionary<string, ICommandDefinition> commands;

    public LorqCommandCatalog(IEnumerable<ICommandDefinition> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        this.commands = commands.ToDictionary(command => command.Name, StringComparer.Ordinal);
    }

    public static LorqCommandCatalog CreateDefault()
    {
        return new LorqCommandCatalog(new ICommandDefinition[]
        {
            new CommandDefinition<RunOptions>("run", LorqCommandOptionsParser.ParseRun, new RunCommandHandler()),
            new CommandDefinition<ValidatePackageOptions>("validate-package", LorqCommandOptionsParser.ParseValidatePackage, new ValidatePackageCommandHandler()),
            new CommandDefinition<ValidateMergeInputsOptions>("validate-merge-inputs", LorqCommandOptionsParser.ParseValidateMergeInputs, new ValidateMergeInputsCommandHandler()),
            new CommandDefinition<RebuildIndexesOptions>("rebuild-indexes", LorqCommandOptionsParser.ParseRebuildIndexes, new RebuildIndexesCommandHandler()),
            new CommandDefinition<MergeShardsOptions>("merge-shards", LorqCommandOptionsParser.ParseMergeShards, new MergeShardsCommandHandler()),
            new CommandDefinition<JudgePackageOptions>("judge-package", LorqCommandOptionsParser.ParseJudgePackage, new JudgePackageCommandHandler()),
            new CommandDefinition<ReportPackageOptions>("report-package", LorqCommandOptionsParser.ParseReportPackage, new ReportPackageCommandHandler()),
        });
    }

    public bool TryFind(string name, out ICommandDefinition command)
    {
        return commands.TryGetValue(name, out command!);
    }
}
