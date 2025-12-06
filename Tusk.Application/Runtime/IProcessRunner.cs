namespace Tusk.Application.Runtime;

public interface IProcessRunner
{
    Task<int> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string?>? environment = null,
        bool redirectOutput = false,
        bool redirectError = false,
        CancellationToken cancellationToken = default);
}
