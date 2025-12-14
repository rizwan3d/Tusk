using System.Text.Json;
using System.Text.Json.Serialization;
using Ivory.Application.Diagnostics;
using SysEnv = System.Environment;

namespace Ivory.Infrastructure.Diagnostics;

public sealed class TelemetryService : ITelemetryService
{
    private readonly string _logPath;
    public bool IsEnabled { get; }

    public TelemetryService()
    {
        var home = SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile);
        var ivoryHome = Path.Combine(home, ".ivory");
        var logDir = Path.Combine(ivoryHome, "logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "telemetry.log");

        bool optInFile = File.Exists(Path.Combine(ivoryHome, "telemetry.optin"));
        bool optInEnv = string.Equals(SysEnv.GetEnvironmentVariable("IVORY_TELEMETRY"), "1", StringComparison.Ordinal);
        IsEnabled = optInFile || optInEnv;
    }

    public Task RecordCommandAsync(string commandName, bool success, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        var payload = new TelemetryRecord(DateTimeOffset.UtcNow, commandName, success);

        var json = JsonSerializer.Serialize(payload, TelemetryJsonContext.Default.TelemetryRecord);
        File.AppendAllText(_logPath, json + SysEnv.NewLine);
        return Task.CompletedTask;
    }
}

public record TelemetryRecord(DateTimeOffset ts, string command, bool success);

[JsonSerializable(typeof(TelemetryRecord))]
internal partial class TelemetryJsonContext : JsonSerializerContext;

