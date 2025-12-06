using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Tusk.Domain.Php;

namespace Tusk.Cli.Formatting;

internal static class ConsoleFormatter
{
    public static void PrintVersions(IEnumerable<PhpVersion> versions, bool asJson)
    {
        if (asJson)
        {
            var payload = versions.Select(v => v.Value).ToArray();

            var json = JsonSerializer.Serialize(payload, CliJsonContext.Default.StringArray);
            Console.WriteLine(json);
            return;
        }

        foreach (var v in versions.OrderBy(v => v.Value))
        {
            Console.WriteLine($"  - {v.Value}");
        }
    }

    public static void PrintDoctor(object model, bool asJson)
    {
        if (asJson)
        {
            var json = JsonSerializer.Serialize(model, CliJsonContext.Default.DoctorModel);
            Console.WriteLine(json);
        }
    }
}
