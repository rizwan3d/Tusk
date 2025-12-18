using System.CommandLine;
using System.Text;
using System.Text.Json;
using Ivory.Application.Deploy;
using Ivory.Cli.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class EnvCommand
{
    public static Command Create(IDeployApiClient apiClient, IDeployConfigStore configStore)
    {
        var projectOption = new Option<Guid>("--project-id")
        {
            Description = "Project id to read config for."
        };

        var environmentOption = new Option<ConfigEnvironment>("--env")
        {
            Description = "Configuration environment (Production or Preview).",
            DefaultValueFactory = _ => ConfigEnvironment.Production
        };

        var apiUrlOption = new Option<string>("--api-url")
        {
            Description = "Override API base URL for this command."
        };

        var userIdOption = new Option<string>("--user-id")
        {
            Description = "Override user id for this command."
        };

        var command = new Command("env", "Fetch environment configuration for a project.")
        {
            projectOption,
            environmentOption,
            apiUrlOption,
            userIdOption
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("env", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(
                    configStore,
                    parseResult.GetValue(apiUrlOption),
                    parseResult.GetValue(userIdOption)).ConfigureAwait(false);

                var projectId = parseResult.GetValue(projectOption);
                if (projectId == Guid.Empty)
                {
                    throw new IvoryCliException("Project id is required.");
                }

                var env = parseResult.GetValue(environmentOption);

                var config = await apiClient.GetEnvironmentAsync(session, projectId, env).ConfigureAwait(false);

                CliConsole.Success($"Config for project {projectId} ({env})");
                Console.WriteLine($"PHP version : {config.PhpVersion}");
                Console.WriteLine($"Install     : {config.InstallCommand}");
                Console.WriteLine($"Build       : {config.BuildCommand}");
                Console.WriteLine($"Composer    : install {config.ComposerInstallFlags}");
                Console.WriteLine($"Composer auth: {config.ComposerAuthJson}");
                Console.WriteLine($"Composer cache: {config.ComposerCacheDir}");
                Console.WriteLine($"Framework   : {config.Framework}");
                Console.WriteLine($"PHP extensions: {config.RequiredExtensionsCsv}");
                Console.WriteLine($"php.ini     : {config.PhpIniOverridesJson}");
                Console.WriteLine($"Runtime     : {config.DeploymentMode} (min {config.MinInstances}, max {config.MaxInstances}, conc {config.MaxConcurrency}, cpu {config.CpuLimitCores}, mem {config.MemoryMb}MB)");
                Console.WriteLine($"Health      : {config.HealthCheckPath}, drain={config.EnableRequestDraining}, shutdownGrace={config.ShutdownGraceSeconds}s, ephemeralFs={config.EphemeralFilesystem}");
                Console.WriteLine($"Egress      : internet={(config.AllowInternetEgress ? "allowed" : "blocked")}, external DB={(string.IsNullOrWhiteSpace(config.ExternalDbConnection) ? "not set" : "configured")}");
                Console.WriteLine($"Logs        : streaming={(config.EnableLogStreaming ? "on" : "off")}, buildRetention={config.BuildLogRetentionDays}d, runtimeRetention={config.RuntimeLogRetentionDays}d");
                Console.WriteLine($"Metrics     : {(config.MetricsEnabled ? "on" : "off")} (retention {config.MetricsRetentionDays}d)");
                Console.WriteLine($"Tracing     : {(config.TracingEnabled ? "on" : "off")} {(string.IsNullOrWhiteSpace(config.OtelEndpoint) ? "" : $"endpoint={config.OtelEndpoint}")}");
                Console.WriteLine($"Errors      : {(config.ErrorReportingEnabled ? "on" : "off")} {(string.IsNullOrWhiteSpace(config.SentryDsn) ? "" : "sentry configured")}");
                Console.WriteLine($"Web root    : {config.WebRoot}");
                Console.WriteLine($"Routing     : {config.Routing}");
                Console.WriteLine("Env vars    :");
                Console.WriteLine(PrettyPrintJson(config.EnvVarsJson));

                var secrets = config.Secrets.Select(s => s.Key).ToArray();
                Console.WriteLine("Secrets     : " + (secrets.Length == 0 ? "(none)" : string.Join(", ", secrets)));
            }).ConfigureAwait(false);
        });

        return command;
    }

    private static string PrettyPrintJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "{}";
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                doc.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return raw;
        }
    }
}
