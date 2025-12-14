using System.Text;
using System.Text.Json;
using Ivory.Domain.Config;

namespace Ivory.Cli.Helpers;

internal static class IvoryConfigSerialization
{
    public static string SerializeIvoryConfig(IvoryConfig config)
    {
        return IvoryConfigSerializer.Serialize(config);
    }

    public static void WriteIvoryConfigObject(Utf8JsonWriter writer, IvoryConfig config)
    {
        IvoryConfigSerializer.Write(writer, config);
    }
}

