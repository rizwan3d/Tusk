using Tusk.Domain.Php;

namespace Tusk.Application.Php;

public interface IPhpVersionResolver
{
    Task<PhpVersion> ResolveForCurrentDirectoryAsync(CancellationToken cancellationToken = default);

    Task SetDefaultAsync(PhpVersion version, CancellationToken cancellationToken = default);
}
