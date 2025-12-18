using System.Text.Json;
using Ivory.Application.Deploy;
using SysEnv = System.Environment;

namespace Ivory.Infrastructure.Deploy;

public sealed class DeployConfigStore : IDeployConfigStore
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

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
            var config = await JsonSerializer.DeserializeAsync<DeployCliConfig>(stream, _options, cancellationToken).ConfigureAwait(false);
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
        await JsonSerializer.SerializeAsync(stream, config, _options, cancellationToken).ConfigureAwait(false);
    }
}
