using System.Diagnostics;
using Tusk.Application.Runtime;

namespace Tusk.Infrastructure.Runtime;

public class ProcessExecution : IProcessRunner
{
    public async Task<int> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string?>? environment = null,
        bool redirectOutput = false,
        bool redirectError = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executable))
            throw new ArgumentException("Executable must not be null or empty.", nameof(executable));

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectError,
            RedirectStandardInput = false
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory!;
        }

        if (environment is not null)
        {
            foreach (var kvp in environment)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value is not null)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
                          ?? throw new InvalidOperationException($"Failed to start process '{executable}'.");

        if (redirectOutput)
        {
            _ = Task.Run(async () =>
            {
                while (!process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    if (line is null) break;
                    Console.Out.WriteLine(line);
                }
            }, cancellationToken);
        }

        if (redirectError)
        {
            _ = Task.Run(async () =>
            {
                while (!process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    var line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
                    if (line is null) break;
                    Console.Error.WriteLine(line);
                }
            }, cancellationToken);
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
