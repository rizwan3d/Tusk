using Ivory.Application.Php;
using Ivory.Domain.Config;

namespace Ivory.Application.Composer;

public interface IComposerService
{
    string? FindComposerConfig(string? configRoot);
    Task<string?> EnsureComposerPharAsync(CancellationToken cancellationToken = default);
    string? FindComposerPhar(string? configRoot);
    Task<int> RunComposerAsync(
        string[] args,
        string phpVersionSpec,
        IvoryConfig? config,
        string? configRoot,
        CancellationToken cancellationToken = default);
    Task<int> RunComposerScriptAsync(
        string scriptName,
        string[] extraArgs,
        string phpVersionSpec,
        IvoryConfig? config,
        string? configRoot,
        CancellationToken cancellationToken = default);
}

