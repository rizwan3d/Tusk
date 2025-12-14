using Ivory.Application.Environment;

namespace Ivory.Infrastructure.Environment;

public class EnvironmentProbe : IEnvironmentProbe
{
    public string? FindSystemPhpExecutablePath()
    {
        string exeName = OperatingSystem.IsWindows() ? "php.exe" : "php";
        var pathEnv = System.Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            string candidate = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public string? FindSystemComposerExecutablePath()
    {
        string[] names = OperatingSystem.IsWindows()
            ? ["composer.bat", "composer.exe", "composer"]
            : ["composer"];

        var pathEnv = System.Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            string trimmed = dir.Trim();
            foreach (var name in names)
            {
                string candidate = Path.Combine(trimmed, name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}

