namespace Tusk.Application.Environment;

public interface IEnvironmentProbe
{
    string? FindSystemPhpExecutablePath();
    string? FindSystemComposerExecutablePath();
}
