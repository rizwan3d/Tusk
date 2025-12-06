using System.CommandLine;
using System.CommandLine.Invocation;
using Tusk.Domain.Php;

namespace Tusk.Cli.Commands;

internal static class UseCommand
{
    public static Command Create()
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version (e.g. 8.3, 8.2, latest)."
        };

        var command = new Command("use", "Use a PHP version for this project (.tusk.php-version).")
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
            await Console.Out.WriteLineAsync($"[tusk] Setting PHP {version} for {Environment.CurrentDirectory}...").ConfigureAwait(false);
            var path = Path.Combine(Environment.CurrentDirectory, ".tusk.php-version");
            await File.WriteAllTextAsync(path, version.Value).ConfigureAwait(false);
        });

        return command;
    }
}
