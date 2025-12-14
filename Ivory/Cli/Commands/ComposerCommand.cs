using System.CommandLine;
using System.CommandLine.Invocation;
using Ivory.Application.Composer;
using Ivory.Application.Config;
using Ivory.Domain.Config;
using Ivory.Cli.Execution;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class ComposerCommand
{
    public static Command Create(IComposerService composerService, Option<string> phpVersionOption, IProjectConfigProvider configProvider)
    {
        var composerArgs = new Argument<string[]>("args")
        {
            Description = "Arguments to pass to Composer.",
            Arity = ArgumentArity.ZeroOrMore
        };

        var command = new Command("composer", "Run Composer using the resolved PHP version.\nExamples:\n  ivory composer install\n  ivory composer --php 8.3 -- update")
        {
            composerArgs
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("composer", async _ =>
            {
                string phpVersionSpec = parseResult.GetValue(phpVersionOption) ?? string.Empty;
                string[] argsToComposer = parseResult.GetValue(composerArgs) ?? [];

                var configResult = await configProvider.LoadAsync(Environment.CurrentDirectory).ConfigureAwait(false);
                CliConsole.Info($"Running Composer (php={phpVersionSpec}).");
                await composerService.RunComposerAsync(
                    argsToComposer,
                    phpVersionSpec,
                    configResult.Config,
                    configResult.RootDirectory,
                    CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);
        });

        return command;
    }
}

