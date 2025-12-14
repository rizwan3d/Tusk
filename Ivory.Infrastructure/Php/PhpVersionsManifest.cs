using System.Text.Json;
using Ivory.Domain.Runtime;

namespace Ivory.Infrastructure.Php;

public sealed class PhpVersionsManifest
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, PlatformEntry> Platforms { get; set; } = [];

    public static PhpVersionsManifest LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"php-versions.json not found at '{path}'.", path);
        }

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var manifest = new PhpVersionsManifest
        {
            SchemaVersion = root.GetProperty("schemaVersion").GetInt32(),
            Platforms = new Dictionary<string, PlatformEntry>(StringComparer.OrdinalIgnoreCase)
        };

        if (root.TryGetProperty("platforms", out var platformsElem) &&
            platformsElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var platformProp in platformsElem.EnumerateObject())
            {
                var platformId = platformProp.Name;
                var platformElem = platformProp.Value;

                var entry = new PlatformEntry
                {
                    Versions = new Dictionary<string, PhpArtifact>(StringComparer.OrdinalIgnoreCase),
                    Aliases  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };

                if (platformElem.TryGetProperty("versions", out var versionsElem) &&
                    versionsElem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var verProp in versionsElem.EnumerateObject())
                    {
                        var verName = verProp.Name;
                        var verElem = verProp.Value;

                        var artifact = new PhpArtifact
                        {
                            Url = verElem.GetProperty("url").GetString() ?? string.Empty,
                            Sha256 = verElem.GetProperty("sha256").GetString() ?? string.Empty
                        };

                        entry.Versions[verName] = artifact;
                    }
                }

                if (platformElem.TryGetProperty("aliases", out var aliasesElem) &&
                    aliasesElem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var aliasProp in aliasesElem.EnumerateObject())
                    {
                        entry.Aliases[aliasProp.Name] = aliasProp.Value.GetString() ?? string.Empty;
                    }
                }

                manifest.Platforms[platformId] = entry;
            }
        }

        return manifest;
    }

    public bool TryResolveArtifact(
        PlatformId platform,
        string spec,
        out string resolvedVersion,
        out PhpArtifact artifact)
    {
        resolvedVersion = "";
        artifact = new PhpArtifact();

        if (string.IsNullOrWhiteSpace(spec))
        {
            return false;
        }

        if (!Platforms.TryGetValue(platform.Value, out var entry))
        {
            return false;
        }

        if (entry.Aliases.TryGetValue(spec, out var aliasTarget) &&
            !string.IsNullOrWhiteSpace(aliasTarget))
        {
            spec = aliasTarget!;
        }

        if (entry.Versions.TryGetValue(spec, out var foundArtifact) && foundArtifact is not null)
        {
            artifact = foundArtifact;
            resolvedVersion = spec;
            return true;
        }

        var candidates = entry.Versions.Keys
            .Where(v => v.StartsWith(spec + ".", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count > 0)
        {
            resolvedVersion = candidates[0];
            artifact = entry.Versions[resolvedVersion];
            return true;
        }

        return false;
    }
}

public sealed class PlatformEntry
{
    public Dictionary<string, PhpArtifact> Versions { get; set; } = [];
    public Dictionary<string, string> Aliases { get; set; } = [];
}

public sealed class PhpArtifact
{
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
}

