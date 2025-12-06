namespace Tusk.Domain.Cli.Doctor;

public sealed class AvailableRequest
{
    public required string version { get; init; }
    public required string file { get; init; }
    public required string sha256 { get; init; }
}
