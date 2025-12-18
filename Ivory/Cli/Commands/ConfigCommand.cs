using System.CommandLine;
using System.Text;
using System.Text.Json;
using Ivory.Application.Config;
using Ivory.Application.Deploy;
using Ivory.Cli.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;
using Ivory.Domain.Config;

namespace Ivory.Cli.Commands;

internal static class ConfigCommand
{
    private static readonly string[] RequiredExtensions = ["pdo", "mbstring", "openssl", "json", "curl", "ctype", "tokenizer", "xml"];

    public static Command Create(IDeployApiClient apiClient, IDeployConfigStore configStore, IProjectConfigProvider configProvider)
    {
        var projectOption = new Option<Guid>("--project-id")
        {
            Description = "Project id to sync configuration to."
        };

        var envOption = new Option<ConfigEnvironment>("--env")
        {
            Description = "Configuration environment (Production or Preview).",
            DefaultValueFactory = _ => ConfigEnvironment.Production
        };

        var sync = new Command("sync", "Push ivory.json settings to the deploy API.")
        {
            projectOption,
            envOption
        };

        sync.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("config:sync", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(configStore, parseResult.GetValue(CommonOptions.ApiUrl), parseResult.GetValue(CommonOptions.UserId)).ConfigureAwait(false);

                var projectId = parseResult.GetValue(projectOption);
                if (projectId == Guid.Empty)
                {
                    throw new IvoryCliException("Project id is required.");
                }

                var env = parseResult.GetValue(envOption);

                var projectConfig = await configProvider.LoadAsync(Environment.CurrentDirectory).ConfigureAwait(false);
                if (!projectConfig.Found || projectConfig.Config is null || projectConfig.RootDirectory is null)
                {
                    throw new IvoryCliException("No ivory.json found. Run this command inside a project with ivory.json.");
                }

                var request = BuildRequest(projectConfig.Config, projectConfig.RootDirectory);

                await apiClient.UpsertConfigAsync(session, projectId, env, request).ConfigureAwait(false);

                CliConsole.Success($"Synced ivory.json to project {projectId} ({env}).");
                Console.WriteLine($"PHP: {request.PhpVersion}");
                Console.WriteLine($"Extensions: {request.RequiredExtensionsCsv}");
                Console.WriteLine($"php.ini: {request.PhpIniOverridesJson}");
            }).ConfigureAwait(false);
        });

        var command = new Command("config", "Manage deploy configuration.");
        command.Options.Add(CommonOptions.ApiUrl);
        command.Options.Add(CommonOptions.UserId);
        command.Subcommands.Add(sync);
        return command;
    }

    private static ConfigUpsertRequest BuildRequest(IvoryConfig config, string rootDirectory)
    {
        var phpVersion = (config.Php.Version ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phpVersion))
        {
            throw new IvoryCliException("php.version is required in ivory.json.");
        }

        var iniJson = BuildPhpIniOverrides(config.Php.Ini);
        var framework = DetectFramework(rootDirectory);

        const string install = "composer install --no-dev --no-interaction --prefer-dist";
        const string build = "composer dump-autoload -o";
        const string composerFlags = "--no-dev --no-interaction --prefer-dist";
        var extensionsCsv = string.Join(",", RequiredExtensions);

        return new ConfigUpsertRequest
        {
            EnvVarsJson = "{}",
            PhpVersion = phpVersion,
            InstallCommand = install,
            BuildCommand = build,
            ComposerInstallFlags = composerFlags,
            ComposerAuthJson = "{}",
            ComposerCacheDir = ".composer/cache",
            OpcacheEnabled = true,
            Framework = framework,
            RequiredExtensionsCsv = extensionsCsv,
            PhpIniOverridesJson = iniJson,
            DeploymentMode = DeploymentMode.ServerlessContainer,
            MinInstances = 0,
            MaxInstances = 5,
            MaxConcurrency = 20,
            CpuLimitCores = 0.25,
            MemoryMb = 512,
            HealthCheckPath = "/health",
            ShutdownGraceSeconds = 20,
            EphemeralFilesystem = true,
            EnableRequestDraining = true,
            AllowInternetEgress = true,
            ExternalDbConnection = string.Empty,
            EnableLogStreaming = true,
            BuildLogRetentionDays = 7,
            RuntimeLogRetentionDays = 14,
            MetricsEnabled = true,
            MetricsRetentionDays = 14,
            TracingEnabled = false,
            OtelEndpoint = null,
            OtelHeaders = null,
            ErrorReportingEnabled = false,
            SentryDsn = null,
            WebRoot = "public",
            Routing = RoutingMode.Server
        };
    }

    private static string BuildPhpIniOverrides(IEnumerable<string> entries)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in entries ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var parts = raw.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new IvoryCliException($"Invalid php.ini override '{raw}'. Expected key=value.");
            }

            map[parts[0]] = parts[1];
        }

        if (map.Count == 0)
        {
            return "{}";
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var kvp in map)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string DetectFramework(string rootDirectory)
    {
        var artisan = Path.Combine(rootDirectory, "artisan");
        if (File.Exists(artisan)) return "laravel";

        var symfonyConsole = Path.Combine(rootDirectory, "bin", "console");
        if (File.Exists(symfonyConsole)) return "symfony";

        return "generic";
    }
}
