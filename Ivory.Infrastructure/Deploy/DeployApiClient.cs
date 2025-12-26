using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Ivory.Application.Deploy;
using Ivory.Infrastructure.Http;

namespace Ivory.Infrastructure.Deploy;

public sealed class DeployApiClient : IDeployApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DeployApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<LoginResult> LoginAsync(DeploySession session, string? tokenName, CancellationToken cancellationToken = default)
    {
        return SendAsync<LoginResult>(
            session,
            HttpMethod.Post,
            "/cli/login",
            new LoginRequest { Name = tokenName },
            GetTypeInfo<LoginResult>(),
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<LoginRequest>());
    }

    public async Task<IReadOnlyList<OrgSummary>> GetOrgsAsync(DeploySession session, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<List<OrgSummary>>(
            session,
            HttpMethod.Get,
            "/orgs",
            null,
            GetTypeInfo<List<OrgSummary>>(),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    public Task<OrgSummary> CreateOrgAsync(DeploySession session, string name, CancellationToken cancellationToken = default)
    {
        return SendAsync<OrgSummary>(
            session,
            HttpMethod.Post,
            "/orgs",
            new CreateOrgRequest { Name = name },
            GetTypeInfo<OrgSummary>(),
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<CreateOrgRequest>());
    }

    public async Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(DeploySession session, string orgName, CancellationToken cancellationToken = default)
    {
        var org = EnsureOrg(orgName);
        var result = await SendAsync<List<ProjectSummary>>(
            session,
            HttpMethod.Get,
            $"/orgs/{Segment(org)}/projects",
            null,
            GetTypeInfo<List<ProjectSummary>>(),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    public Task<ProjectSummary> CreateProjectAsync(DeploySession session, string orgName, string name, CancellationToken cancellationToken = default)
    {
        var org = EnsureOrg(orgName);
        return SendAsync<ProjectSummary>(
            session,
            HttpMethod.Post,
            $"/orgs/{Segment(org)}/projects",
            new CreateProjectRequest { Name = name },
            GetTypeInfo<ProjectSummary>(),
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<CreateProjectRequest>());
    }

    public Task<DeploymentCreated> CreateDeploymentAsync(
        DeploySession session,
        string orgName,
        string projectName,
        DeploymentEnvironment environment,
        string? branch,
        string? commitSha,
        string? artifactLocation,
        CancellationToken cancellationToken = default)
    {
        var (org, project) = EnsureNames(orgName, projectName);
        return SendAsync<DeploymentCreated>(
            session,
            HttpMethod.Post,
            "/cli/deploy",
            new CreateDeploymentRequest
            {
                OrgName = org,
                ProjectName = project,
                Environment = environment,
                Branch = branch,
                CommitSha = commitSha,
                ArtifactLocation = artifactLocation
            },
            GetTypeInfo<DeploymentCreated>(),
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<CreateDeploymentRequest>());
    }

    public async Task<UploadedArtifact> UploadArtifactAsync(
        DeploySession session,
        string orgName,
        string projectName,
        string version,
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
        {
            throw new FileNotFoundException("Archive not found.", archivePath);
        }

        var (org, project) = EnsureNames(orgName, projectName);
        var baseUri = BuildBaseUri(session.ApiBaseUrl);
        var requestUri = new Uri(baseUri, "/cli/artifacts/upload");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(org), "orgName");
        content.Add(new StringContent(project), "projectName");
        content.Add(new StringContent(version), "version");

        await using var stream = File.OpenRead(archivePath);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", Path.GetFileName(archivePath));

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };
        request.Headers.Add("X-User-Email", session.UserEmail);

        using var client = _httpClientFactory.CreateClient(HttpClientNames.Deploy);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
            var message = $"API request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
            if (!string.IsNullOrWhiteSpace(detail))
            {
                message += $" {detail}";
            }

            throw new InvalidOperationException(message);
        }

        if (response.Content is null)
        {
            throw new InvalidOperationException("API response was empty.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync(responseStream, GetTypeInfo<UploadedArtifact>(), cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            throw new InvalidOperationException("Failed to parse API response.");
        }

        return result;
    }

    public Task<DeploymentLogInfo> GetLogsAsync(DeploySession session, Guid deploymentId, CancellationToken cancellationToken = default)
    {
        return SendAsync<DeploymentLogInfo>(
            session,
            HttpMethod.Get,
            $"/cli/logs/{deploymentId}",
            null,
            GetTypeInfo<DeploymentLogInfo>(),
            cancellationToken);
    }

    public Task<EnvConfigResult> GetEnvironmentAsync(DeploySession session, string orgName, string projectName, ConfigEnvironment environment, CancellationToken cancellationToken = default)
    {
        var (org, project) = EnsureNames(orgName, projectName);
        var envSegment = environment.ToString();
        return SendAsync<EnvConfigResult>(
            session,
            HttpMethod.Get,
            $"/cli/env/{Segment(org)}/{Segment(project)}/{envSegment}",
            null,
            GetTypeInfo<EnvConfigResult>(),
            cancellationToken);
    }

    public Task<IReadOnlyList<DomainInfo>> GetDomainsAsync(DeploySession session, string orgName, string projectName, CancellationToken cancellationToken = default)
    {
        var (org, project) = EnsureNames(orgName, projectName);
        return SendDomainsAsync(session, org, project, cancellationToken);
    }

    public async Task UpsertConfigAsync(
        DeploySession session,
        string orgName,
        string projectName,
        ConfigEnvironment environment,
        ConfigUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var (org, project) = EnsureNames(orgName, projectName);
        var envSegment = environment.ToString();
        await SendAsync(
            session,
            HttpMethod.Post,
            $"/orgs/{Segment(org)}/projects/{Segment(project)}/config/{envSegment}",
            request,
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<ConfigUpsertRequest>(useEnumStrings: false)).ConfigureAwait(false);
    }

    public Task<RollbackResult> RollbackAsync(DeploySession session, string orgName, string projectName, Guid targetDeploymentId, CancellationToken cancellationToken = default)
    {
        var (org, project) = EnsureNames(orgName, projectName);
        return SendAsync<RollbackResult>(
            session,
            HttpMethod.Post,
            "/cli/rollback",
            new RollbackRequest
            {
                OrgName = org,
                ProjectName = project,
                TargetDeploymentId = targetDeploymentId
            },
            GetTypeInfo<RollbackResult>(),
            cancellationToken,
            payloadTypeInfo: GetTypeInfo<RollbackRequest>());
    }

    public async Task<RegisterResult> RegisterUserAsync(string apiBaseUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException("API base URL is required.");
        }

        return await SendAsync<RegisterResult>(
            new DeploySession(apiBaseUrl, string.Empty), // no auth header needed for register
            HttpMethod.Post,
            "/users/register",
            new RegisterUserRequest { Email = email, Password = password },
            responseTypeInfo: GetTypeInfo<RegisterResult>(),
            cancellationToken: cancellationToken,
            includeUserHeader: false,
            payloadTypeInfo: GetTypeInfo<RegisterUserRequest>()).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DomainInfo>> SendDomainsAsync(DeploySession session, string orgName, string projectName, CancellationToken cancellationToken)
    {
        var domains = await SendAsync<List<DomainInfo>>(
            session,
            HttpMethod.Get,
            $"/cli/domains/{Segment(orgName)}/{Segment(projectName)}",
            null,
            GetTypeInfo<List<DomainInfo>>(),
            cancellationToken).ConfigureAwait(false);

        return domains;
    }

    private async Task<T> SendAsync<T>(
        DeploySession session,
        HttpMethod method,
        string path,
        object? payload,
        JsonTypeInfo<T> responseTypeInfo,
        CancellationToken cancellationToken,
        bool includeUserHeader = true,
        JsonTypeInfo? payloadTypeInfo = null)
    {
        var baseUri = BuildBaseUri(session.ApiBaseUrl);
        var requestUri = new Uri(baseUri, path);

        using var request = new HttpRequestMessage(method, requestUri);
        if (includeUserHeader)
        {
            request.Headers.Add("X-User-Email", session.UserEmail);
        }

        if (payload is not null)
        {
            if (payloadTypeInfo is null)
            {
                throw new InvalidOperationException($"No JSON type info registered for payload type '{payload.GetType().Name}'.");
            }

            request.Content = JsonContent.Create(payload, payloadTypeInfo);
        }

        using var client = _httpClientFactory.CreateClient(HttpClientNames.Deploy);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
            var message = $"API request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
            if (!string.IsNullOrWhiteSpace(detail))
            {
                message += $" {detail}";
            }

            throw new InvalidOperationException(message);
        }

        if (response.Content is null)
        {
            throw new InvalidOperationException("API response was empty.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync(stream, responseTypeInfo, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            throw new InvalidOperationException("Failed to parse API response.");
        }

        return result;
    }

    private async Task SendAsync(
        DeploySession session,
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken,
        bool includeUserHeader = true,
        JsonTypeInfo? payloadTypeInfo = null)
    {
        var baseUri = BuildBaseUri(session.ApiBaseUrl);
        var requestUri = new Uri(baseUri, path);

        using var request = new HttpRequestMessage(method, requestUri);
        if (includeUserHeader)
        {
            request.Headers.Add("X-User-Email", session.UserEmail);
        }

        if (payload is not null)
        {
            if (payloadTypeInfo is null)
            {
                throw new InvalidOperationException($"No JSON type info registered for payload type '{payload.GetType().Name}'.");
            }

            request.Content = JsonContent.Create(payload, payloadTypeInfo);
        }

        using var client = _httpClientFactory.CreateClient(HttpClientNames.Deploy);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await SafeReadAsync(response, cancellationToken).ConfigureAwait(false);
            var message = $"API request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
            if (!string.IsNullOrWhiteSpace(detail))
            {
                message += $" {detail}";
            }

            throw new InvalidOperationException(message);
        }
    }

    private static string EnsureOrg(string orgName)
    {
        if (string.IsNullOrWhiteSpace(orgName))
        {
            throw new InvalidOperationException("Org name is required.");
        }

        return orgName.Trim();
    }

    private static (string Org, string Project) EnsureNames(string orgName, string projectName)
    {
        if (string.IsNullOrWhiteSpace(orgName) || string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException("Org and project names are required.");
        }

        return (orgName.Trim(), projectName.Trim());
    }

    private static string Segment(string value) => Uri.EscapeDataString(value ?? string.Empty);

    private static Uri BuildBaseUri(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid API base URL '{baseUrl}'.");
        }

        return uri;
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>(bool useEnumStrings = true)
    {
        var context = useEnumStrings ? (JsonSerializerContext)DeployJsonContext.Default : DeployJsonNumericContext.Default;
        return (JsonTypeInfo<T>?)context.GetTypeInfo(typeof(T))
            ?? throw new InvalidOperationException($"No JSON type info registered for '{typeof(T).Name}'.");
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

}
