using System.Diagnostics.CodeAnalysis;
using Ivory.Domain.Config;

namespace Ivory.Application.Config;

public interface IProjectConfigProvider
{
    Task<ProjectConfigResult> LoadAsync(string startDirectory, CancellationToken cancellationToken = default);
}

public sealed record ProjectConfigResult(IvoryConfig? Config, string? RootDirectory)
{
    [MemberNotNullWhen(true, nameof(Config), nameof(RootDirectory))]
    public bool Found => Config is not null && RootDirectory is not null;
}

