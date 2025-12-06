using System.Diagnostics;
using Tusk.Application.Config;
using Tusk.Domain.Config;

namespace Tusk.Infrastructure.Config;

public sealed class ProjectConfigProvider : IProjectConfigProvider
{
    public async Task<ProjectConfigResult> LoadAsync(string startDirectory, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetFullPath(startDirectory);

        while (!string.IsNullOrWhiteSpace(dir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = Path.Combine(dir, "tusk.json");
            if (File.Exists(candidate))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(candidate, cancellationToken).ConfigureAwait(false);
                    if (TuskConfigSerializer.TryDeserialize(json, out var config) && config is not null)
                    {
                        return new ProjectConfigResult(config, dir);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load tusk.json from {candidate}: {ex}");
                }
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return new ProjectConfigResult(null, null);
    }
}
