using System.CommandLine;
using System.CommandLine.Invocation;
using Tusk.Application.Php;

namespace Tusk.Cli.Commands;

internal static class ListCommand
{
    public static Command Create(IPhpInstaller installer)
    {
        var command = new Command("list", "List installed PHP versions.");
        command.Aliases.Add("ls");

        command.SetAction(async _ =>
        {
            var versions = await installer.ListInstalledAsync().ConfigureAwait(false);
            Console.WriteLine("[tusk] Installed PHP versions:");
            if (versions.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
            else
            {
                foreach (var v in versions.OrderBy(v => v.Value))
                {
                    Console.WriteLine($"  {v.Value}");
                }
            }
        });

        return command;
    }
}
