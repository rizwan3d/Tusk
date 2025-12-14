using System.Diagnostics;
using Ivory.Application.Config;
using Ivory.Domain.Config;

namespace Ivory.Infrastructure.Config;

public sealed class ProjectConfigProvider : IProjectConfigProvider
{
    public async Task<ProjectConfigResult> LoadAsync(string startDirectory, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetFullPath(startDirectory);

        while (!string.IsNullOrWhiteSpace(dir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = Path.Combine(dir, "ivory.json");
            if (File.Exists(candidate))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(candidate, cancellationToken).ConfigureAwait(false);
                    if (IvoryConfigSerializer.TryDeserialize(json, out var config) && config is not null)
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
                    Debug.WriteLine($"Failed to load ivory.json from {candidate}: {ex}");
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

