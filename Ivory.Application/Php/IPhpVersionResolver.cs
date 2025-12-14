using Ivory.Domain.Php;

namespace Ivory.Application.Php;

public interface IPhpVersionResolver
{
    Task<PhpVersion> ResolveForCurrentDirectoryAsync(CancellationToken cancellationToken = default);

    Task SetDefaultAsync(PhpVersion version, CancellationToken cancellationToken = default);
}

