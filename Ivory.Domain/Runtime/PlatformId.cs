using System.Runtime.InteropServices;

namespace Ivory.Domain.Runtime;

public readonly record struct PlatformId(string Value)
{
    public static PlatformId DetectCurrent()
    {
        string osPart = GetOsPart();
        string archPart = GetArchPart();
        return new PlatformId($"{osPart}-{archPart}");
    }

    private static string GetOsPart()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macos";

        throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
    }

    private static string GetArchPart()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64   => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException(
                $"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
        };
    }

    public override string ToString() => Value;
}

