namespace Ivory.Application.Deploy;

public interface IDeployConfigStore
{
    Task<DeployCliConfig> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DeployCliConfig config, CancellationToken cancellationToken = default);
}
