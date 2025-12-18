namespace Ivory.Application.Deploy;

public sealed class ConfigUpsertRequest
{
    public string? EnvVarsJson { get; set; }
    public string? PhpVersion { get; set; }
    public string? InstallCommand { get; set; }
    public string? BuildCommand { get; set; }
    public string? ComposerInstallFlags { get; set; }
    public string? ComposerAuthJson { get; set; }
    public string? ComposerCacheDir { get; set; }
    public bool? OpcacheEnabled { get; set; }
    public string? Framework { get; set; }
    public string? RequiredExtensionsCsv { get; set; }
    public string? PhpIniOverridesJson { get; set; }
    public DeploymentMode? DeploymentMode { get; set; }
    public int? MinInstances { get; set; }
    public int? MaxInstances { get; set; }
    public int? MaxConcurrency { get; set; }
    public double? CpuLimitCores { get; set; }
    public int? MemoryMb { get; set; }
    public string? HealthCheckPath { get; set; }
    public int? ShutdownGraceSeconds { get; set; }
    public bool? EphemeralFilesystem { get; set; }
    public bool? EnableRequestDraining { get; set; }
    public bool? AllowInternetEgress { get; set; }
    public string? ExternalDbConnection { get; set; }
    public bool? EnableLogStreaming { get; set; }
    public int? BuildLogRetentionDays { get; set; }
    public int? RuntimeLogRetentionDays { get; set; }
    public bool? MetricsEnabled { get; set; }
    public int? MetricsRetentionDays { get; set; }
    public bool? TracingEnabled { get; set; }
    public string? OtelEndpoint { get; set; }
    public string? OtelHeaders { get; set; }
    public bool? ErrorReportingEnabled { get; set; }
    public string? SentryDsn { get; set; }
    public string? WebRoot { get; set; }
    public RoutingMode Routing { get; set; }
}
