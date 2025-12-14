using System.CommandLine;
using System.CommandLine.Invocation;
using Ivory.Cli.Execution;
using Ivory.Cli.Formatting;
using Ivory.Application.Php;

namespace Ivory.Cli.Commands;

internal static class PruneCommand
{
    public static Command Create(IPhpInstaller installer)
    {
        var keepOption = new Option<int>("--keep")
        {
            Description = "How many latest versions to keep.",
            DefaultValueFactory = (e) => 1,
        };
        var includeCacheOption = new Option<bool>("--include-cache")
        {
            Description = "Whether to clear cached archives.",
            DefaultValueFactory = (e) => true,
        };

        var command = new Command("prune", "Prune old PHP versions and optional caches.")
        {
            keepOption,
            includeCacheOption
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("prune", async _ =>
            {
                int keep = parseResult.GetValue(keepOption);
                bool includeCache = parseResult.GetValue(includeCacheOption);
                CliConsole.Info("Pruning old PHP installations...");
                var removed = await installer.PruneAsync(keep, includeCache).ConfigureAwait(false);
                CliConsole.Success($"Pruned {removed} installation(s).");
            }).ConfigureAwait(false);
        });

        return command;
    }
}

