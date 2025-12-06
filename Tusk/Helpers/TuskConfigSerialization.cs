using System.Text;
using System.Text.Json;
using Tusk.Domain.Config;

namespace Tusk.Cli.Helpers;

internal static class TuskConfigSerialization
{
    public static string SerializeTuskConfig(TuskConfig config)
    {
        return TuskConfigSerializer.Serialize(config);
    }

    public static void WriteTuskConfigObject(Utf8JsonWriter writer, TuskConfig config)
    {
        TuskConfigSerializer.Write(writer, config);
    }
}
