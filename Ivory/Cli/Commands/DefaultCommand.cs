using System.CommandLine;
using System.CommandLine.Invocation;
using Ivory.Application.Php;
using Ivory.Domain.Php;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class DefaultCommand
{
    public static Command Create(IPhpVersionResolver resolver)
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version (e.g. 8.3, 8.2)."
        };

        var command = new Command("default", "Set the default/global PHP version.\nExamples:\n  ivory default 8.3\n  ivory default system")
        {
            versionArgument
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("default", async context =>
            {
                var versionText = parseResult.GetValue(versionArgument) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(versionText))
                {
                    throw new IvoryCliException("Version cannot be empty.");
                }

                var version = new PhpVersion(versionText);
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".ivory",
                    "config.json");

                string? existingConfig = null;
                if (File.Exists(configPath))
                {
                    existingConfig = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                }

                context.OnRollback(async () =>
                {
                    if (existingConfig is not null)
                    {
                        await File.WriteAllTextAsync(configPath, existingConfig).ConfigureAwait(false);
                    }
                    else if (File.Exists(configPath))
                    {
                        File.Delete(configPath);
                    }
                });

                CliConsole.Info($"Setting default PHP version to {version}...");
                await resolver.SetDefaultAsync(version).ConfigureAwait(false);
                CliConsole.Success($"Default PHP version set to {version}.");
            }).ConfigureAwait(false);
        });

        return command;
    }
}

