using Tusk.Domain.Php;

namespace Tusk.Application.Php;

public interface IPhpInstaller
{
    Task InstallAsync(PhpVersion version, CancellationToken cancellationToken = default);

    Task<string> GetInstalledPathAsync(PhpVersion version, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhpVersion>> ListInstalledAsync(CancellationToken cancellationToken = default);
    void Dispose();
}
