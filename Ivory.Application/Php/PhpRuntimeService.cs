using Ivory.Application.Runtime;
using Ivory.Domain.Php;

namespace Ivory.Application.Php;

public class PhpRuntimeService(
    IPhpInstaller installer,
    IPhpVersionResolver resolver,
    IProcessRunner processRunner,
    IProjectPhpHomeProvider projectPhpHomeProvider) : IPhpRuntimeService
{
    private readonly IPhpInstaller _installer = installer;
    private readonly IPhpVersionResolver _resolver = resolver;
    private readonly IProcessRunner _processRunner = processRunner;
    private readonly IProjectPhpHomeProvider _projectPhpHomeProvider = projectPhpHomeProvider;

    public Task<int> RunPhpAsync(
        string? scriptOrCommand,
        string[] args,
        string? overrideVersionSpec,
        CancellationToken cancellationToken = default)
        => RunPhpInternalAsync(scriptOrCommand, args, overrideVersionSpec, null, cancellationToken);

    public Task<int> RunPhpAsync(
        string? scriptOrCommand,
        string[] args,
        string? overrideVersionSpec,
        IDictionary<string, string?>? environment,
        CancellationToken cancellationToken = default)
        => RunPhpInternalAsync(scriptOrCommand, args, overrideVersionSpec, environment, cancellationToken);

    private async Task<int> RunPhpInternalAsync(
        string? scriptOrCommand,
        string[] args,
        string? overrideVersionSpec,
        IDictionary<string, string?>? environment,
        CancellationToken cancellationToken)
    {
        PhpVersion version;
        if (!string.IsNullOrWhiteSpace(overrideVersionSpec))
        {
            if (string.Equals(overrideVersionSpec.Trim(), "system", StringComparison.OrdinalIgnoreCase))
            {
                version = new PhpVersion("system");
            }
            else
            {
                version = new PhpVersion(overrideVersionSpec.Trim());
            }
        }
        else
        {
            version = await _resolver.ResolveForCurrentDirectoryAsync(cancellationToken).ConfigureAwait(false);
        }

        string phpPath;
        if (string.Equals(version.Value, "system", StringComparison.OrdinalIgnoreCase))
        {
            phpPath = OperatingSystem.IsWindows() ? "php.exe" : "php";
        }
        else
        {
            phpPath = await EnsureInstalledPhpAsync(version, cancellationToken).ConfigureAwait(false);
        }

        var iniOverride = ResolveIniOverride(phpPath);
        string? extensionDirForIni = null;
        try
        {
            var phpDir = Path.GetDirectoryName(phpPath);
            if (!string.IsNullOrWhiteSpace(phpDir))
            {
                var extCandidate = Path.Combine(phpDir, "ext");
                if (Directory.Exists(extCandidate))
                {
                    extensionDirForIni = extCandidate;
                }
            }
        }
        catch
        {
        }

        var finalArgs = new List<string>();
        if (!string.IsNullOrWhiteSpace(iniOverride))
        {
            finalArgs.Add("-c");
            finalArgs.Add(iniOverride!);
        }
        TryAddExtensionDirArg(phpPath, args, finalArgs);

        if (!string.IsNullOrEmpty(scriptOrCommand))
        {
            finalArgs.Add(scriptOrCommand);
        }

        if (args is not null && args.Length > 0)
        {
            finalArgs.AddRange(args);
        }

        var env = environment is not null
            ? new Dictionary<string, string?>(environment, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        env["IVORY_PHP"] = phpPath;
        ConfigurePhpEnvironment(phpPath, env);

        var projectHome = await _projectPhpHomeProvider
            .TryGetExistingAsync(System.Environment.CurrentDirectory, cancellationToken)
            .ConfigureAwait(false);

        if (projectHome is not null)
        {
            env["PHPRC"] = Path.GetDirectoryName(projectHome.IniPath) ?? projectHome.IniPath;
            env["PHP_INI_SCAN_DIR"] = projectHome.ExtensionsPath;
            env["IVORY_PHP_HOME"] = projectHome.HomePath;
            if (projectHome.ProjectRoot is not null)
            {
                env["IVORY_PROJECT_ROOT"] = projectHome.ProjectRoot;
            }

            if (extensionDirForIni is not null)
            {
                WriteExtensionDirIni(projectHome.ExtensionsPath, extensionDirForIni);
            }
        }
        else
        {
            env.TryAdd("IVORY_PROJECT_ROOT", System.Environment.CurrentDirectory);
        }

        string workingDir = System.Environment.CurrentDirectory;

        return await _processRunner.RunAsync(
            executable: phpPath,
            arguments: finalArgs,
            workingDirectory: workingDir,
            environment: env,
            redirectOutput: false,
            redirectError: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static void TryAddExtensionDirArg(string phpPath, string[]? args, List<string> targetArgs)
    {
        try
        {
            var phpDir = Path.GetDirectoryName(phpPath);
            if (string.IsNullOrWhiteSpace(phpDir))
            {
                return;
            }

            var extDir = Path.Combine(phpDir, "ext");
            if (!Directory.Exists(extDir))
            {
                return;
            }

            if (args is not null && args.Any(a => a.Contains("extension_dir", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            string extensionArg = $"extension_dir=\"{extDir}\"";
            targetArgs.Add("-d");
            targetArgs.Add(extensionArg);
        }
        catch
        {
            // Ignore optional extension_dir auto-injection failures.
        }
    }

    private async Task<string> EnsureInstalledPhpAsync(PhpVersion version, CancellationToken cancellationToken)
    {
        try
        {
            return await _installer.GetInstalledPathAsync(version, cancellationToken).ConfigureAwait(false);
        }
        catch (System.IO.FileNotFoundException)
        {
            Console.WriteLine($"[ivory] PHP {version.Value} is not installed. Installing now...");
            await _installer.InstallAsync(version, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await _installer.GetInstalledPathAsync(version, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ConfigurePhpEnvironment(string phpPath, IDictionary<string, string?> env)
    {
        try
        {
            var phpDir = Path.GetDirectoryName(phpPath);
            if (string.IsNullOrWhiteSpace(phpDir))
            {
                return;
            }

            var extensionDir = Path.Combine(phpDir, "ext");
            if (Directory.Exists(extensionDir))
            {
                env["PHP_EXTENSION_DIR"] = extensionDir;
            }

            // Ensure a PHP ini scan directory exists with an extension_dir override so child PHP processes inherit it.
            var defaultScanDir = Path.Combine(phpDir, "conf.d");
            Directory.CreateDirectory(defaultScanDir);
            WriteExtensionDirIni(defaultScanDir, extensionDir);

            env.TryAdd("PHP_INI_SCAN_DIR", defaultScanDir);

            var phpIniPath = Path.Combine(phpDir, "php.ini");
            var phpIniDir = Path.GetDirectoryName(phpIniPath);
            if (!string.IsNullOrWhiteSpace(phpIniDir))
            {
                env.TryAdd("PHPRC", phpIniDir);
            }

            var separator = OperatingSystem.IsWindows() ? ';' : ':';
            var currentPath = env.TryGetValue("PATH", out var envPath) && !string.IsNullOrWhiteSpace(envPath)
                ? envPath!
                : System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            bool alreadyPresent = currentPath
                .Split([separator], StringSplitOptions.RemoveEmptyEntries)
                .Any(p => string.Equals(p.Trim(), phpDir, StringComparison.OrdinalIgnoreCase));

            var newPath = alreadyPresent
                ? currentPath
                : (string.IsNullOrEmpty(currentPath) ? phpDir : phpDir + separator + currentPath);

            env["PATH"] = newPath;
        }
        catch
        {
            // Best-effort environment setup.
        }
    }

    private static string? ResolveIniOverride(string phpPath)
    {
        try
        {
            var phpDir = Path.GetDirectoryName(phpPath);
            if (string.IsNullOrWhiteSpace(phpDir))
            {
                return null;
            }

            string primary = Path.Combine(phpDir, "php.ini");
            if (File.Exists(primary))
            {
                return primary;
            }

            string dev = Path.Combine(phpDir, "php.ini-development");
            if (File.Exists(dev))
            {
                return dev;
            }

            string prod = Path.Combine(phpDir, "php.ini-production");
            if (File.Exists(prod))
            {
                return prod;
            }
        }
        catch
        {
        }

        return null;
    }

    private static void WriteExtensionDirIni(string scanDir, string extensionDir)
    {
        if (!Directory.Exists(scanDir) || string.IsNullOrWhiteSpace(extensionDir))
        {
            return;
        }

        try
        {
            var iniPath = Path.Combine(scanDir, "99-ivory-extension-dir.ini");
            var desired = $"extension_dir=\"{extensionDir}\"{System.Environment.NewLine}";
            if (!File.Exists(iniPath) || File.ReadAllText(iniPath) != desired)
            {
                File.WriteAllText(iniPath, desired);
            }
        }
        catch
        {
            // Ignore failures; main process still adds -d extension_dir when possible.
        }
    }
}
