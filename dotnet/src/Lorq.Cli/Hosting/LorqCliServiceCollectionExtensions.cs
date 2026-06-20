using Lorq.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Lorq.Cli.Commands.Handlers;
using Lorq.Cli.Commands.Parsing;

namespace Lorq.Cli.Hosting;

public static class LorqCliServiceCollectionExtensions
{
    public static IServiceCollection AddLorqCli(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ICommandHandler<RunOptions>, RunCommandHandler>();
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
