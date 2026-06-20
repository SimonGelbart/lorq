using Microsoft.Extensions.DependencyInjection;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands;

internal static class LorqCommandCatalogFactory
{
    public static LorqCommandCatalog Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new LorqCommandCatalog(new ICommandDefinition[]
        {
            Command<RunOptions>(services, "run", LorqCommandOptionsParser.ParseRun),
            Command<ValidatePackageOptions>(services, "validate-package", LorqCommandOptionsParser.ParseValidatePackage),
            Command<ValidateMergeInputsOptions>(services, "validate-merge-inputs", LorqCommandOptionsParser.ParseValidateMergeInputs),
            Command<RebuildIndexesOptions>(services, "rebuild-indexes", LorqCommandOptionsParser.ParseRebuildIndexes),
            Command<MergeShardsOptions>(services, "merge-shards", LorqCommandOptionsParser.ParseMergeShards),
            Command<JudgePackageOptions>(services, "judge-package", LorqCommandOptionsParser.ParseJudgePackage),
            Command<ReportPackageOptions>(services, "report-package", LorqCommandOptionsParser.ParseReportPackage),
        });
    }

    private static ICommandDefinition Command<TOptions>(
        IServiceProvider services,
        string name,
        Func<IReadOnlyList<string>, ParseResult<TOptions>> parse)
        where TOptions : CommandOptions
    {
        return new CommandDefinition<TOptions>(name, parse, services.GetRequiredService<ICommandHandler<TOptions>>());
    }
}
