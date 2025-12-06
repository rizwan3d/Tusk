namespace Tusk.Domain.Config;

public sealed class TuskConfig
{
    public PhpSection Php { get; init; } = new();
    public Dictionary<string, TuskScript> Scripts { get; init; }
        = new(StringComparer.OrdinalIgnoreCase);

    public sealed class PhpSection
    {
        public string? Version { get; set; }
        public List<string> Ini { get; init; } = [];
        public List<string> Args { get; init; } = [];
    }

    public sealed class TuskScript
    {
        public string? Description { get; init; }
        public string PhpFile { get; init; } = "";
        public List<string> PhpArgs { get; init; } = [];
        public List<string> Args { get; init; } = [];
    }
}

public enum FrameworkKind
{
    Generic,
    Laravel,
    Symfony
}

public static class TuskConfigFactory
{
    public static TuskConfig CreateFor(FrameworkKind framework)
        => framework switch
        {
            FrameworkKind.Laravel => CreateLaravel(),
            FrameworkKind.Symfony => CreateSymfony(),
            _ => CreateGeneric()
        };

    private static TuskConfig CreateGeneric()
    {
        return new TuskConfig
        {
            Php = new TuskConfig.PhpSection
            {
                Version = "8.3",
                Ini = { "display_errors=1" }
            },
            Scripts = new Dictionary<string, TuskConfig.TuskScript>(StringComparer.OrdinalIgnoreCase)
            {
                ["serve"] = new()
                {
                    Description = "Run built-in PHP dev server (public/index.php)",
                    PhpFile = "public/index.php",
                    PhpArgs = { "-S", "localhost:8000" },
                    Args = { "--env=dev" }
                },
                ["test"] = new()
                {
                    Description = "Run PHPUnit tests",
                    PhpFile = "vendor/bin/phpunit",
                    Args = { "--colors=always" }
                }
            }
        };
    }

    private static TuskConfig CreateLaravel()
    {
        return new TuskConfig
        {
            Php = new TuskConfig.PhpSection
            {
                Version = "8.3",
                Ini = { "display_errors=1" }
            },
            Scripts = new Dictionary<string, TuskConfig.TuskScript>(StringComparer.OrdinalIgnoreCase)
            {
                ["serve"] = new()
                {
                    Description = "Run Laravel development server",
                    PhpFile = "artisan",
                    Args = { "serve" }
                },
                ["migrate"] = new()
                {
                    Description = "Run database migrations",
                    PhpFile = "artisan",
                    Args = { "migrate" }
                },
                ["tinker"] = new()
                {
                    Description = "Open Laravel Tinker REPL",
                    PhpFile = "artisan",
                    Args = { "tinker" }
                },
                ["queue:work"] = new()
                {
                    Description = "Run Laravel queue worker",
                    PhpFile = "artisan",
                    Args = { "queue:work" }
                }
            }
        };
    }

    private static TuskConfig CreateSymfony()
    {
        return new TuskConfig
        {
            Php = new TuskConfig.PhpSection
            {
                Version = "8.3",
                Ini = { "display_errors=1" }
            },
            Scripts = new Dictionary<string, TuskConfig.TuskScript>(StringComparer.OrdinalIgnoreCase)
            {
                ["serve"] = new()
                {
                    Description = "Run Symfony dev server using PHP built-in server",
                    PhpFile = "public/index.php",
                    PhpArgs = { "-S", "127.0.0.1:8000" }
                },
                ["console"] = new()
                {
                    Description = "Run bin/console",
                    PhpFile = "bin/console"
                },
                ["migrations:migrate"] = new()
                {
                    Description = "Run Doctrine migrations",
                    PhpFile = "bin/console",
                    Args = { "doctrine:migrations:migrate" }
                }
            }
        };
    }
}
