using Microsoft.Extensions.Hosting;

namespace Lorq.Cli.Hosting;

public static class LorqCliHost
{
    public static IHost Build(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddLorqCli();
        return builder.Build();
    }
}
