using System.CommandLine;
using System.CommandLine.Invocation;
using Tusk.Application.Php;
using Tusk.Domain.Php;

namespace Tusk.Cli.Commands;

internal static class UninstallCommand
{
    public static Command Create(IPhpInstaller installer)
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version to uninstall."
        };

        var command = new Command("uninstall", "Remove an installed PHP version safely.")
        {
            versionArgument
        };

        command.SetAction(async parseResult =>
        {
            var versionText = parseResult.GetValue(versionArgument) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(versionText))
            {
                await Console.Error.WriteLineAsync("[tusk] Version cannot be empty.").ConfigureAwait(false);
                return;
            }

            var version = new PhpVersion(versionText);
            await installer.UninstallAsync(version).ConfigureAwait(false);
        });

        return command;
    }
}
