using Tusk.Application.Php;
using Tusk.Domain.Config;

namespace Tusk.Application.Composer;

public interface IComposerService
{
    string? FindComposerConfig(string? configRoot);
    Task<string?> EnsureComposerPharAsync(CancellationToken cancellationToken = default);
    string? FindComposerPhar(string? configRoot);
    Task<int> RunComposerAsync(
        string[] args,
        string phpVersionSpec,
        TuskConfig? config,
        string? configRoot,
        CancellationToken cancellationToken = default);
    Task<int> RunComposerScriptAsync(
        string scriptName,
        string[] extraArgs,
        string phpVersionSpec,
        TuskConfig? config,
        string? configRoot,
        CancellationToken cancellationToken = default);
}
