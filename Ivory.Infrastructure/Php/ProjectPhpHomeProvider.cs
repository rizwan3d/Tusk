using System.Text;
using Ivory.Application.Config;
using Ivory.Application.Php;

namespace Ivory.Infrastructure.Php;

public sealed class ProjectPhpHomeProvider(IProjectConfigProvider configProvider) : IProjectPhpHomeProvider
{
    private readonly IProjectConfigProvider _configProvider = configProvider;

    public async Task<ProjectPhpHome> EnsureCreatedAsync(string startDirectory, CancellationToken cancellationToken = default)
    {
        var configResult = await _configProvider.LoadAsync(startDirectory, cancellationToken).ConfigureAwait(false);
        string projectRoot = configResult.RootDirectory ?? Path.GetFullPath(startDirectory);

        string home = Path.Combine(projectRoot, ".ivory", "php");
        string iniPath = Path.Combine(home, "php.ini");
        string confDir = Path.Combine(home, "conf.d");

        Directory.CreateDirectory(home);
        Directory.CreateDirectory(confDir);

        if (!File.Exists(iniPath))
        {
            var content = new StringBuilder();
            content.AppendLine("; Ivory per-project php.ini");
            content.AppendLine("; Add overrides here to keep settings local to this project.");
            File.WriteAllText(iniPath, content.ToString());
        }

        return new ProjectPhpHome(home, iniPath, confDir, configResult.RootDirectory);
    }

    public async Task<ProjectPhpHome?> TryGetExistingAsync(string startDirectory, CancellationToken cancellationToken = default)
    {
        var configResult = await _configProvider.LoadAsync(startDirectory, cancellationToken).ConfigureAwait(false);
        string projectRoot = configResult.RootDirectory ?? Path.GetFullPath(startDirectory);

        string home = Path.Combine(projectRoot, ".ivory", "php");
        string iniPath = Path.Combine(home, "php.ini");
        string confDir = Path.Combine(home, "conf.d");

        if (!Directory.Exists(home) || !File.Exists(iniPath))
        {
            return null;
        }

        Directory.CreateDirectory(confDir);

        return new ProjectPhpHome(home, iniPath, confDir, configResult.RootDirectory);
    }
}

