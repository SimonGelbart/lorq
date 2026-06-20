using Lorq.Cli;
using Microsoft.Extensions.DependencyInjection;
using Lorq.Cli.Hosting;

using var host = LorqCliHost.Build(args);
var application = host.Services.GetRequiredService<LorqCliApplication>();
return await application.ExecuteAsync(args, Console.Out, Console.Error);
