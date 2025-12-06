using Tusk.Domain.Php;

namespace Tusk.Application.Php;

public interface IPhpInstaller
{
    Task InstallAsync(PhpVersion version, bool ignoreChecksum = false, CancellationToken cancellationToken = default);

    Task<string> GetInstalledPathAsync(PhpVersion version, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhpVersion>> ListInstalledAsync(CancellationToken cancellationToken = default);

    Task UninstallAsync(PhpVersion version, CancellationToken cancellationToken = default);

    Task<int> PruneAsync(int keepLatest = 1, bool includeCache = true, CancellationToken cancellationToken = default);
    void Dispose();
}
