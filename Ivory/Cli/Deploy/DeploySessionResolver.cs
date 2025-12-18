using Ivory.Application.Deploy;
using Ivory.Cli.Exceptions;

namespace Ivory.Cli.Deploy;

internal static class DeploySessionResolver
{
    public static async Task<DeploySession> ResolveAsync(
        IDeployConfigStore configStore,
        string? apiBaseOverride,
        string? userIdOverride,
        CancellationToken cancellationToken = default)
    {
        var config = await configStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        var apiBase = apiBaseOverride
                      ?? Environment.GetEnvironmentVariable("IVORY_DEPLOY_API")
                      ?? config.ApiBaseUrl;

        if (string.IsNullOrWhiteSpace(apiBase))
        {
            throw new IvoryCliException("Missing API base URL. Pass --api-url or run 'iv login --api-url <url> --user-id <guid>'.");
        }

        var userIdValue = userIdOverride
                          ?? Environment.GetEnvironmentVariable("IVORY_DEPLOY_USER_ID")
                          ?? config.UserId?.ToString();

        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            throw new IvoryCliException("Missing or invalid user id. Pass --user-id or run 'iv login --user-id <guid>'.");
        }

        return new DeploySession(apiBase.Trim(), userId);
    }
}
