using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;
using Tusk.Application.Php;
using Tusk.Domain.Php;
using Tusk.Domain.Runtime;

namespace Tusk.Infrastructure.Php;

public class PhpInstaller : IPhpInstaller, IDisposable
{
    private readonly PhpVersionsManifest _manifest;
    private readonly PlatformId _platform;
    private readonly string _versionsRoot;
    private readonly string _cacheRoot;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public PhpInstaller(PhpVersionsManifest manifest, HttpClient? httpClient = null)
    {
        _manifest = manifest;
        _platform = PlatformId.DetectCurrent();

        var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        var tuskDir = Path.Combine(home, ".tusk");

        _versionsRoot = Path.Combine(tuskDir, "versions");
        _cacheRoot = Path.Combine(tuskDir, "cache", "php");

        Directory.CreateDirectory(_versionsRoot);
        Directory.CreateDirectory(_cacheRoot);

        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task InstallAsync(PhpVersion versionSpec, CancellationToken cancellationToken = default)
    {
        if (!_manifest.TryResolveArtifact(_platform, versionSpec.Value,
                out var resolvedVersion, out var artifact))
        {
            throw new InvalidOperationException(
                $"PHP version '{versionSpec.Value}' is not defined for platform '{_platform.Value}' in php-versions.json.");
        }

        string installDir = GetInstallDir(resolvedVersion);
        if (Directory.Exists(installDir))
        {
            Console.WriteLine($"[tusk] PHP {resolvedVersion} already installed at {installDir}");
            return;
        }

        Console.WriteLine($"[tusk] Installing PHP {resolvedVersion} for {_platform.Value}...");
        Directory.CreateDirectory(installDir);

        string archivePath = await DownloadArchiveAsync(artifact.Url, artifact.Sha256, resolvedVersion, cancellationToken).ConfigureAwait(false);
        ExtractArchive(archivePath, installDir);
        await NormalizePhpLayoutAsync(installDir, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"[tusk] Installed PHP {resolvedVersion} at {installDir}");
    }

    public Task<string> GetInstalledPathAsync(PhpVersion version, CancellationToken cancellationToken = default)
    {
        string installDir = GetInstallDir(version.Value);
        string phpName = OperatingSystem.IsWindows() ? "php.exe" : "php";
        string phpPath = Path.Combine(installDir, phpName);

        if (!File.Exists(phpPath))
        {
            throw new FileNotFoundException($"PHP binary not found at {phpPath}. Is version '{version.Value}' installed?");
        }

        return Task.FromResult(phpPath);
    }

    public Task<IReadOnlyList<PhpVersion>> ListInstalledAsync(CancellationToken cancellationToken = default)
    {
        var platformDir = Path.Combine(_versionsRoot, _platform.Value);
        if (!Directory.Exists(platformDir))
        {
            return Task.FromResult<IReadOnlyList<PhpVersion>>([]);
        }

        var versions = Directory.GetDirectories(platformDir)
            .Select(Path.GetFileName)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => new PhpVersion(v!))
            .ToList();

        return Task.FromResult<IReadOnlyList<PhpVersion>>(versions);
    }

    private string GetInstallDir(string exactVersion)
        => Path.Combine(_versionsRoot, _platform.Value, exactVersion);

    private async Task<string> DownloadArchiveAsync(
        string url,
        string expectedSha256,
        string version,
        CancellationToken ct)
    {
        string versionCacheDir = Path.Combine(_cacheRoot, version);
        Directory.CreateDirectory(versionCacheDir);

        var uri = new Uri(url);
        string fileName = Path.GetFileName(uri.LocalPath);
        string archivePath = Path.Combine(versionCacheDir, fileName);

        if (File.Exists(archivePath))
        {
            if (await VerifySha256Async(archivePath, expectedSha256, ct).ConfigureAwait(false))
            {
                Console.WriteLine($"[tusk] Using cached archive {archivePath}");
                return archivePath;
            }

            Console.WriteLine("[tusk] Cached archive checksum mismatch, deleting...");
            File.Delete(archivePath);
        }

        Console.WriteLine($"[tusk] Downloading {url}...");
        await using (var responseStream = await _httpClient.GetStreamAsync(uri, ct).ConfigureAwait(false))
        await using (var fileStream = File.Create(archivePath))
        {
            await responseStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        }

        if (!await VerifySha256Async(archivePath, expectedSha256, ct).ConfigureAwait(false))
        {
            File.Delete(archivePath);
            throw new InvalidOperationException("Checksum mismatch for downloaded PHP archive.");
        }

        return archivePath;
    }

    private static async Task<bool> VerifySha256Async(string path, string expectedHex, CancellationToken ct)
    {
        expectedHex = expectedHex.Trim().ToLowerInvariant();

        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct).ConfigureAwait(false);

        string actualHex = Convert.ToHexStringLower(hash);
        return string.Equals(actualHex, expectedHex, StringComparison.OrdinalIgnoreCase);
    }

    private void ExtractArchive(string archivePath, string targetDir)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, targetDir, overwriteFiles: true);
        }
        else if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                 archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            ExtractTarGzWithTar(archivePath, targetDir);
        }
        else
        {
            throw new NotSupportedException($"Unsupported archive type: {Path.GetFileName(archivePath)}");
        }
    }

    private static void ExtractTarGzWithTar(string archivePath, string targetDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "tar",
            ArgumentList = { "-xzf", archivePath, "-C", targetDir },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)
                           ?? throw new InvalidOperationException("Failed to start 'tar' process.");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"tar failed with code {process.ExitCode}: {error}");
        }
    }

    private async Task NormalizePhpLayoutAsync(string installDir, CancellationToken ct = default)
    {
        string binDir = Path.Combine(installDir, "bin");
        Directory.CreateDirectory(binDir);

        string phpName = OperatingSystem.IsWindows() ? "php.exe" : "php";

        string? existing = Directory
            .EnumerateFiles(installDir, phpName, SearchOption.AllDirectories)
            .FirstOrDefault() ?? throw new InvalidOperationException("Extracted archive does not contain a PHP binary.");
        string targetPath = Path.Combine(binDir, phpName);

        if (!File.Exists(targetPath))
        {
            File.Move(existing, targetPath);
        }
        else
        {
            if (!string.Equals(existing, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(existing);
            }
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}
