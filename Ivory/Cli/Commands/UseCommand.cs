using System.CommandLine;
using System.CommandLine.Invocation;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;
using Ivory.Domain.Php;

namespace Ivory.Cli.Commands;

internal static class UseCommand
{
    public static Command Create()
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version (e.g. 8.3, 8.2, latest)."
        };

        var command = new Command("use", "Use a PHP version for this project (.ivory.php-version).\nExamples:\n  ivory use 8.3\n  ivory use system")
        {
            versionArgument
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("use", async context =>
            {
                var versionText = parseResult.GetValue(versionArgument) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(versionText))
                {
                    throw new IvoryCliException("Version cannot be empty.");
                }

                var version = new PhpVersion(versionText);
                var path = Path.Combine(Environment.CurrentDirectory, ".ivory.php-version");
                bool existed = File.Exists(path);
                string? previousValue = existed ? await File.ReadAllTextAsync(path).ConfigureAwait(false) : null;

                context.OnRollback(async () =>
                {
                    if (existed && previousValue is not null)
                    {
                        await File.WriteAllTextAsync(path, previousValue).ConfigureAwait(false);
                    }
                    else if (!existed && File.Exists(path))
                    {
                        File.Delete(path);
                    }
                });

                CliConsole.Info($"Setting PHP {version} for {Environment.CurrentDirectory}...");
                await File.WriteAllTextAsync(path, version.Value).ConfigureAwait(false);
                CliConsole.Success($"PHP {version} pinned for this project.");
            }).ConfigureAwait(false);
        });

        return command;
    }
}

