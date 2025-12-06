using System.Text.RegularExpressions;

namespace Tusk.Infrastructure.Php;

/// <summary>
/// Fetches available Windows PHP builds from the official sha256sum feed.
/// Only 64-bit NTS builds are supported.
/// </summary>
public sealed class WindowsPhpFeed
{
    private static readonly Uri ShaListUri = new("https://windows.php.net/downloads/releases/sha256sum.txt");
    private const string DownloadBase = "https://windows.php.net/downloads/releases/";
    private const string UserAgent = "tusk-cli/1.0 (+https://github.com/)";

    private readonly HttpClient _httpClient;
    private Dictionary<string, (string File, string Sha)>? _cache;
    private readonly object _lock = new();

    public WindowsPhpFeed(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    public async Task<IReadOnlyList<(string Version, string File, string Sha)>> ListAsync(CancellationToken ct = default)
    {
        await EnsureCacheAsync(ct).ConfigureAwait(false);
        return _cache!
            .Select(kvp => (kvp.Key, kvp.Value.File, kvp.Value.Sha))
            .OrderByDescending(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<(bool Success, string ResolvedVersion, PhpArtifact Artifact)> ResolveAsync(string spec, CancellationToken ct = default)
    {
        await EnsureCacheAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(spec) || _cache is null || _cache.Count == 0)
        {
            return (false, string.Empty, new PhpArtifact());
        }

        if (_cache.TryGetValue(spec, out var exact))
        {
            return (true, spec, new PhpArtifact { Url = DownloadBase + exact.File, Sha256 = exact.Sha });
        }

        var candidates = _cache.Keys
            .Where(v => v.StartsWith(spec + ".", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count > 0 && _cache.TryGetValue(candidates[0], out var best))
        {
            return (true, candidates[0], new PhpArtifact { Url = DownloadBase + best.File, Sha256 = best.Sha });
        }

        return (false, string.Empty, new PhpArtifact());
    }

    private async Task EnsureCacheAsync(CancellationToken ct)
    {
        if (_cache is not null) return;

        using var request = new HttpRequestMessage(HttpMethod.Get, ShaListUri);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var lines = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (lines.Contains("empty user agent", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("windows.php.net rejected the request due to missing User-Agent. Please retry.");
        }
        var dict = new Dictionary<string, (string File, string Sha)>(StringComparer.OrdinalIgnoreCase);

        // match 64-bit NTS builds: php-<ver>-nts-Win32-vsXX-x64.zip
        var regex = new Regex(@"^(?<sha>[a-fA-F0-9]{64})\s+\*php-(?<ver>[\d\.]+)-nts-Win32-(?<vs>vs\d+)-x64\.zip$", RegexOptions.Compiled);

        foreach (var rawLine in lines.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var match = regex.Match(line);
            if (!match.Success) continue;

            var sha = match.Groups["sha"].Value;
            var ver = match.Groups["ver"].Value;
            var vs = match.Groups["vs"].Value;
            var file = $"php-{ver}-nts-Win32-{vs}-x64.zip";

            dict[ver] = (file, sha);
        }

        lock (_lock)
        {
            _cache ??= dict;
        }
    }
}
