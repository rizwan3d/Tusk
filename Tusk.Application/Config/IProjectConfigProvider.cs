using System.Diagnostics.CodeAnalysis;
using Tusk.Domain.Config;

namespace Tusk.Application.Config;

public interface IProjectConfigProvider
{
    Task<ProjectConfigResult> LoadAsync(string startDirectory, CancellationToken cancellationToken = default);
}

public sealed record ProjectConfigResult(TuskConfig? Config, string? RootDirectory)
{
    [MemberNotNullWhen(true, nameof(Config), nameof(RootDirectory))]
    public bool Found => Config is not null && RootDirectory is not null;
}
