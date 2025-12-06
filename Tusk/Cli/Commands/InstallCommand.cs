using System.CommandLine;
using System.CommandLine.Invocation;
using Tusk.Application.Php;
using Tusk.Domain.Php;

namespace Tusk.Cli.Commands;

internal static class InstallCommand
{
    public static Command Create(IPhpInstaller installer)
    {
        var versionArgument = new Argument<string>("version")
        {
            Description = "PHP version (e.g. 8.3, 8.2, latest)."
        };
        var ignoreChecksumOption = new Option<bool>("--ignore-checksum")
        {
            Description = "Skip archive checksum validation (use with caution).",
            DefaultValueFactory = (e) => true
        };
        var command = new Command("install", "Install a PHP runtime version.")
        {
            versionArgument,
            ignoreChecksumOption
        };
        command.Aliases.Add("i");

        command.SetAction(async parseResult =>
        {
            var versionText = parseResult.GetValue(versionArgument);
            if (string.IsNullOrWhiteSpace(versionText))
            {
                await Console.Error.WriteLineAsync("[tusk] Version cannot be empty.").ConfigureAwait(false);
                return;
            }
            bool ignoreChecksum = parseResult.GetValue(ignoreChecksumOption);
            var version = new PhpVersion(versionText);
            Console.WriteLine($"[tusk] Installing PHP {version}...");
            await installer.InstallAsync(version, ignoreChecksum).ConfigureAwait(false);
        });

        return command;
    }
}
