using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ivory.Application.Deploy;

namespace Ivory.Infrastructure.Deploy;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    Converters = new[]
    {
        typeof(DeploymentEnvironmentConverter),
        typeof(DeploymentStatusConverter),
        typeof(ConfigEnvironmentConverter),
        typeof(DeploymentModeConverter),
        typeof(RoutingModeConverter)
    })]
[JsonSerializable(typeof(LoginResult))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(List<OrgSummary>))]
[JsonSerializable(typeof(CreateOrgRequest))]
[JsonSerializable(typeof(OrgSummary))]
[JsonSerializable(typeof(List<ProjectSummary>))]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(ProjectSummary))]
[JsonSerializable(typeof(DeploymentCreated))]
[JsonSerializable(typeof(CreateDeploymentRequest))]
[JsonSerializable(typeof(UploadedArtifact))]
[JsonSerializable(typeof(DeploymentLogInfo))]
[JsonSerializable(typeof(EnvConfigResult))]
[JsonSerializable(typeof(List<DomainInfo>))]
[JsonSerializable(typeof(RollbackResult))]
[JsonSerializable(typeof(RollbackRequest))]
[JsonSerializable(typeof(RegisterUserRequest))]
[JsonSerializable(typeof(RegisterResult))]
[JsonSerializable(typeof(ConfigUpsertRequest))]
[JsonSerializable(typeof(DeployCliConfig))]
internal partial class DeployJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ConfigUpsertRequest))]
internal partial class DeployJsonNumericContext : JsonSerializerContext;

internal sealed class LoginRequest
{
    public string? Name { get; set; }
}

internal sealed class CreateOrgRequest
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class CreateDeploymentRequest
{
    public string OrgName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DeploymentEnvironment Environment { get; set; }
    public string? Branch { get; set; }
    public string? CommitSha { get; set; }
    public string? ArtifactLocation { get; set; }
}

internal sealed class RollbackRequest
{
    public string OrgName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public Guid TargetDeploymentId { get; set; }
}

internal sealed class RegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

internal sealed class DeploymentEnvironmentConverter : JsonStringEnumConverter<DeploymentEnvironment>
{
    public DeploymentEnvironmentConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
    {
    }
}

internal sealed class DeploymentStatusConverter : JsonStringEnumConverter<DeploymentStatus>
{
    public DeploymentStatusConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
    {
    }
}

internal sealed class ConfigEnvironmentConverter : JsonStringEnumConverter<ConfigEnvironment>
{
    public ConfigEnvironmentConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
    {
    }
}

internal sealed class DeploymentModeConverter : JsonStringEnumConverter<DeploymentMode>
{
    public DeploymentModeConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
    {
    }
}

internal sealed class RoutingModeConverter : JsonStringEnumConverter<RoutingMode>
{
    public RoutingModeConverter() : base(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
    {
    }
}
