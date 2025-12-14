using System.Text.Json;
using Ivory.Application.Config;
using Ivory.Application.Php;
using Ivory.Domain.Php;

namespace Ivory.Infrastructure.Php;

public class PhpVersionResolver(IProjectConfigProvider configProvider) : IPhpVersionResolver
{
    private readonly string _configPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".ivory",
            "config.json");
    private readonly IProjectConfigProvider _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

    public async Task<PhpVersion> ResolveForCurrentDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var configResult = await _configProvider.LoadAsync(System.Environment.CurrentDirectory, cancellationToken).ConfigureAwait(false);
        if (configResult.Config?.Php?.Version is { } projectVersion &&
            !string.IsNullOrWhiteSpace(projectVersion))
        {
            return new PhpVersion(projectVersion.Trim());
        }

        if (File.Exists(_configPath))
        {
            var json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("defaultPhpVersion", out var prop))
            {
                var defaultVersion = prop.GetString();
                if (!string.IsNullOrWhiteSpace(defaultVersion))
                {
                    return new PhpVersion(defaultVersion.Trim());
                }
            }
        }

        return new PhpVersion("system");
    }

    public async Task SetDefaultAsync(PhpVersion version, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

        string json = "{\n  \"defaultPhpVersion\": \"" + version.Value + "\"\n}\n";
        await File.WriteAllTextAsync(_configPath, json, cancellationToken).ConfigureAwait(false);
    }
}

