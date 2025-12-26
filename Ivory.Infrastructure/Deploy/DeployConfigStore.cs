using System.Text.Json;
using Ivory.Application.Deploy;
using SysEnv = System.Environment;

namespace Ivory.Infrastructure.Deploy;

public sealed class DeployConfigStore : IDeployConfigStore
{
    private readonly string _configPath;

    public DeployConfigStore()
    {
        var home = SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile);
        _configPath = Path.Combine(home, ".ivory", "deploy.json");
    }

    public async Task<DeployCliConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configPath))
        {
            return new DeployCliConfig();
        }

        try
        {
            await using var stream = File.OpenRead(_configPath);
            var config = await JsonSerializer.DeserializeAsync(stream, DeployJsonContext.Default.DeployCliConfig, cancellationToken).ConfigureAwait(false);
            return config ?? new DeployCliConfig();
        }
        catch
        {
            return new DeployCliConfig();
        }
    }

    public async Task SaveAsync(DeployCliConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

        await using var stream = File.Create(_configPath);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        JsonSerializer.Serialize(writer, config, DeployJsonContext.Default.DeployCliConfig);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
