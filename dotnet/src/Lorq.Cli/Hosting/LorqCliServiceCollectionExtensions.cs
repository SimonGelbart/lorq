using Lorq.Adapters.Process;
using Lorq.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Lorq.Cli.Commands.Handlers;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Runtime;

namespace Lorq.Cli.Hosting;

public static class LorqCliServiceCollectionExtensions
{
    public static IServiceCollection AddLorqCli(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<RunAdapterFactory>();
        services.AddSingleton<RunWorkspacePlanner>();
        services.AddSingleton<RunWorkspaceMaterializer>();
        services.AddSingleton<RunPromptBuilder>();
        services.AddSingleton<RunCellEvidenceFactory>();
        services.AddSingleton<RunCellExecutor>();
        services.AddSingleton<RunShardResultWriter>();
        services.AddSingleton<DeterministicRunShardApplication>();
        services.AddSingleton<FileAdapterConformanceRunner>();
        services.AddSingleton<ICommandHandler<RunOptions>, RunCommandHandler>();
        services.AddSingleton<ICommandHandler<AdapterConformanceOptions>, AdapterConformanceCommandHandler>();
        services.AddSingleton<ICommandHandler<ValidatePackageOptions>, ValidatePackageCommandHandler>();
        services.AddSingleton<ICommandHandler<ValidateMergeInputsOptions>, ValidateMergeInputsCommandHandler>();
        services.AddSingleton<ICommandHandler<RebuildIndexesOptions>, RebuildIndexesCommandHandler>();
        services.AddSingleton<ICommandHandler<MergeShardsOptions>, MergeShardsCommandHandler>();
        services.AddSingleton<ICommandHandler<JudgePackageOptions>, JudgePackageCommandHandler>();
        services.AddSingleton<ICommandHandler<ReportPackageOptions>, ReportPackageCommandHandler>();
        services.AddSingleton(LorqCommandCatalogFactory.Create);
        services.AddSingleton<LorqCliApplication>();
        return services;
    }
}
