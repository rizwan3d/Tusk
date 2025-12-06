using System.CommandLine;
using System.CommandLine.Invocation;
using Tusk.Application.Php;
using Tusk.Domain.Php;

namespace Tusk.Cli.Commands;

internal static class DefaultCommand
{
    public static Command Create(IPhpVersionResolver resolver)
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version (e.g. 8.3, 8.2)."
        };

        var command = new Command("default", "Set the default/global PHP version.")
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
            await Console.Out.WriteLineAsync($"[tusk] Setting default PHP version to {version}...").ConfigureAwait(false);
            await resolver.SetDefaultAsync(version).ConfigureAwait(false);
        });

        return command;
    }
}
