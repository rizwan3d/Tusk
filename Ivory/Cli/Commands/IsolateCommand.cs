using System.CommandLine;
using System.CommandLine.Invocation;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;
using Ivory.Application.Php;

namespace Ivory.Cli.Commands;

internal static class IsolateCommand
{
    public static Command Create(IProjectPhpHomeProvider provider)
    {
        var command = new Command("isolate", "Create a per-project PHP home with its own php.ini and conf.d directory.\nExamples:\n  ivory isolate");

        command.SetAction(async _ =>
        {
            await CommandExecutor.RunAsync("isolate", async _ =>
            {
                string cwd = Environment.CurrentDirectory;
                var home = await provider.EnsureCreatedAsync(cwd).ConfigureAwait(false);

                CliConsole.Success($"Created per-project PHP home at {home.HomePath}");
                CliConsole.Info("Add overrides to php.ini or drop extension .ini files into conf.d/");
            }).ConfigureAwait(false);
        });

        return command;
    }
}

