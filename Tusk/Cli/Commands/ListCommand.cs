using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using Tusk.Application.Php;
using Tusk.Cli.Formatting;

namespace Tusk.Cli.Commands;

internal static class ListCommand
{
    public static Command Create(IPhpInstaller installer)
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output machine-readable JSON."
        };
        var command = new Command("list", "List installed PHP versions.")
        {
            jsonOption
        };
        command.Aliases.Add("ls");

        command.SetAction(async parseResult =>
        {
            bool asJson = parseResult.GetValue(jsonOption);
            var versions = await installer.ListInstalledAsync().ConfigureAwait(false);
            Console.WriteLine("[tusk] Installed PHP versions:");
            if (versions.Count == 0 && !asJson)
            {
                Console.WriteLine("  (none)");
                return;
            }

            Formatting.ConsoleFormatter.PrintVersions(versions, asJson);
        });

        return command;
    }
}
