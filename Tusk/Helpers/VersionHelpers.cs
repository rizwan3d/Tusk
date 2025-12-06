using System.IO;
using Tusk.Application.Php;

namespace Tusk.Cli.Helpers;

internal static class VersionHelpers
{
    public static async Task<string?> DetectPhpVersionFromPhpVAsync(
        IPhpVersionResolver resolver,
        string phpVersionSpec)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(phpVersionSpec) &&
                !string.Equals(phpVersionSpec.Trim(), "system", StringComparison.OrdinalIgnoreCase))
            {
                return phpVersionSpec.Trim();
            }

            var resolved = await resolver.ResolveForCurrentDirectoryAsync().ConfigureAwait(false);
            return resolved.Value;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
