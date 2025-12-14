namespace Ivory.Application.Php;

public interface IPhpRuntimeService
{
    Task<int> RunPhpAsync(
        string? scriptOrCommand,
        string[] args,
        string? overrideVersionSpec,
        CancellationToken cancellationToken = default);

    Task<int> RunPhpAsync(
        string? scriptOrCommand,
        string[] args,
        string? overrideVersionSpec,
        IDictionary<string, string?>? environment,
        CancellationToken cancellationToken = default);
}

