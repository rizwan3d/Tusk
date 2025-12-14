# Ivory

Ivory is a cross-platform CLI tool that manages **local PHP runtimes** and runs **Composer** and PHP scripts in a consistent, project-aware way.

It lets you:

- Install and manage multiple PHP versions under `~/.ivory`
- Pick a **default (global)** PHP version and **per-project** version
- Generate a `ivory.json` project config
- Run PHP directly via Ivory
- Proxy Composer commands through a known PHP version
- Define reusable “scripts” in `ivory.json` and run them easily
- Inspect your environment and check what Ivory sees (`ivory doctor`)

---

## Table of contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration (`ivory.json`)](#configuration-ivoryjson)
- [Commands](#commands)
  - [`ivory init`](#ivory-init)
  - [`ivory install`](#ivory-install)
  - [`ivory list`](#ivory-list)
  - [`ivory php`](#ivory-php)
  - [`ivory run`](#ivory-run)
  - [`ivory scripts`](#ivory-scripts)
  - [`ivory use`](#ivory-use)
  - [`ivory default`](#ivory-default)
  - [`ivory composer`](#ivory-composer)
  - [`ivory isolate`](#ivory-isolate)
  - [`ivory scaffold:ci`](#ivory-scaffoldci)
  - [`ivory scaffold:docker`](#ivory-scaffolddocker)
  - [`ivory completion`](#ivory-completion)
  - [`ivory doctor`](#ivory-doctor)
- [Data & directories](#data--directories)
- [License](#license)

---

## Features

- **Local PHP version management**
  - Installs PHP builds under `~/.ivory/versions/<platform>/<version>`
  - Uses a JSON manifest (`~/.ivory/php-versions.json`) to know where to download PHP from
  - `ivory install 8.3` etc.

- **Project-aware PHP selection**
  - Per-project PHP version via `.ivory.php-version`
  - Global default PHP version via `ivory default`
  - Falls back to system `php` when appropriate

- **Project configuration with `ivory.json`**
  - `php.version`, extra INI and CLI args
  - Named scripts that describe how to run your app/tools
  - Framework presets (Generic, Laravel, Symfony) via `ivory init`

- **Composer integration**
  - Finds or downloads a `composer.phar`
  - Runs Composer using a specific PHP version
  - Can run Composer scripts through your configured PHP toolchain

  - **Nice DX**
    - Uses `System.CommandLine` for a clean CLI
    - Uses `ivory doctor` to show what Ivory sees (PHP, Composer, config, etc.)
    - Scaffolds a basic `public/index.php` that tries to load `vendor/autoload.php`
    - Optional per-project PHP home (`.ivory/php`) to keep ini overrides local

---

## Requirements

- **.NET SDK** with support for `net10.0` (Ivory targets `net10.0` in `Ivory.csproj`).
- Supported runtime platforms:
  - Windows x64
  - Linux x64
  - macOS arm64 (Apple Silicon)
---

## Installation

### Quick install (prebuilt binaries)

Downloads come from the latest release at `https://github.com/rizwan3d/Ivory/releases/latest/download/<platform-archive>`.

```bash
# Linux (x64)
bash <(curl -fsSL https://raw.githubusercontent.com/rizwan3d/Ivory/refs/heads/master/install/linux.sh)

# macOS (arm64 or x64)
bash <(curl -fsSL https://raw.githubusercontent.com/rizwan3d/Ivory/refs/heads/master/install/mac.sh)
```

```powershell
# Windows (PowerShell 5+; AMD64 or ARM64)
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/rizwan3d/Ivory/refs/heads/master/install/windows.ps1 | iex"
```

The installers place `ivory` under a user-scoped bin directory (`~/.ivory/bin` on Unix, `%LOCALAPPDATA%\Ivory\bin` on Windows) and add it to `PATH`.

### Uninstall

```bash
# Linux/macOS
bash <(curl -fsSL https://raw.githubusercontent.com/rizwan3d/Ivory/refs/heads/master/install/uninstall.sh)
```

```powershell
# Windows (PowerShell 5+)
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/rizwan3d/Ivory/refs/heads/master/install/uninstall.ps1 | iex"
```

---

## Configuration (`ivory.json`)

Ivory looks for a `ivory.json` starting from the current directory and walking up parent directories.

### Example `ivory.json`

```json
{
  "php": {
    "version": "8.3",
    "ini": [
      "memory_limit=512M"
    ],
    "args": [
      "-d", "date.timezone=UTC"
    ]
  },
  "scripts": {
    "serve": {
      "description": "Run PHP built-in server",
      "phpFile": "-S",
      "phpArgs": [
        "localhost:8000",
        "public/index.php"
      ],
      "args": []
    },
    "console": {
      "description": "Run framework console",
      "phpFile": "bin/console",
      "phpArgs": [],
      "args": []
    }
  }
}
```

### Schema (simplified)

* **`php`**

  * `version` (string, optional) - preferred PHP version for this project (e.g. `"8.3"` or `"latest"`).
  * `ini` (array of string) - extra `-d` INI settings to pass to PHP.
  * `args` (array of string) - extra arguments always passed to PHP.

* **`scripts`** (object)

  * keys = script names (e.g. `"serve"`, `"test"`, `"console"`).
  * values = **IvoryScript**:

    * `description` (string, optional) – human-readable description.
    * `phpFile` (string, **required**) – the PHP entry file or command (e.g. `"public/index.php"`, `"bin/console"`, `"-S"`).
    * `phpArgs` (array of string, optional) – arguments placed **before** the `phpFile` or used as raw PHP arguments.
    * `args` (array of string, optional) – additional arguments appended after `phpFile` / `phpArgs`.

Internally, these are represented by `IvoryConfig` and `IvoryConfig.IvoryScript` in `Ivory.Domain.Config`.

---

## Commands

### `ivory init`

Create a starter `ivory.json` for the current project and scaffold `public/index.php` if it doesn’t exist.

```bash
ivory init [--framework <Generic|Laravel|Symfony>] [--php <version>] [--force]
```

* `--framework` – choose a preset (Generic, Laravel, Symfony).
* `--php` – default PHP version to write into `ivory.json`.
* `--force` – overwrite an existing `ivory.json`.

This uses `IPhpVersionResolver`, `IvoryConfigSerializer` and `IPublicIndexScaffolder` under the hood.

---

### `ivory install`

Install a PHP runtime for the current platform.

```bash
ivory install <version>
```

Examples:

```bash
ivory install 8.3
ivory install 8.2
```

This calls `IPhpInstaller.InstallAsync`, which:

* Looks up the version in `~/.ivory/php-versions.json` (`PhpVersionsManifest`).
* Downloads the appropriate artifact.
* Verifies SHA256.
* Extracts it into `~/.ivory/versions/<platform>/<version>`.

---

### `ivory list` / `ivory ls`

List installed PHP versions:

```bash
ivory list
# or
ivory ls
```

Uses `IPhpInstaller.ListInstalledAsync()` and prints out versions in order.

---

### `ivory php`

Run `php` through Ivory with a specific PHP version (or project/global default).

```bash
ivory php [--php <version>] [--] [args...]
```

Examples:

```bash
ivory php -- -v
ivory php --php 8.3 -- -v
ivory php --php 8.3 -- -r "echo 'Hello from Ivory';"
```

This calls `IPhpRuntimeService.RunPhpAsync` with `null` script path and just forwards your arguments.

---

### `ivory run`

Run a named script from `ivory.json` or a direct PHP file.

```bash
ivory run <script-or-file> [--php <version>] [--] [extra-args...]
```

Behavior:

1. Ivory loads the nearest `ivory.json` via `IProjectConfigProvider`.
2. If `<script-or-file>` matches a script name in `scripts`, it:

   * Builds the PHP command from `php`, `phpFile`, `phpArgs`, and `args`.
3. Otherwise, it treats `<script-or-file>` as a path to a PHP file relative to the current directory.

Examples:

```bash
ivory run serve
ivory run console -- migrate
ivory run public/index.php --php 8.3
```

---

### `ivory scripts`

List available scripts from `ivory.json`:

```bash
ivory scripts
```

Shows:

* Script name
* Description
* The effective PHP command line that would be executed.

---

### `ivory use`

Set the **project-local** PHP version by writing `.ivory.php-version` in the current directory:

```bash
ivory use <version>
```

Example:

```bash
ivory use 8.3
```

Ivory will prefer this version when running commands inside this project directory.

---

### `ivory default`

Set the **default / global** PHP version (e.g. stored in `~/.ivory/config.json` or equivalent):

```bash
ivory default <version>
```

Example:

```bash
ivory default 8.2
```

Used when there is no `.ivory.php-version` and no `php.version` in `ivory.json`.

---

### `ivory composer`

Run Composer through Ivory.

```bash
ivory composer [--php <version>] [--] [args...]
```

Examples:

```bash
ivory composer install
ivory composer update
ivory composer run-script test
ivory composer --php 8.3 -- install
```

Internally, `IComposerService`:

* Locates `composer.json` and project root.
* Ensures a `composer.phar` is available (downloading if needed).
* Uses `IPhpRuntimeService` to run `php composer.phar <args>` with the chosen PHP version.

---

### `ivory isolate`

Create a per-project PHP home in `.ivory/php/` with its own `php.ini` and `conf.d/` directory for extension ini files.

```bash
ivory isolate
```

After running this once in a project, Ivory will automatically set `PHPRC` and `PHP_INI_SCAN_DIR` to use your local `php.ini` and `conf.d/` when executing PHP/Composer through Ivory, keeping settings and extensions from bleeding across projects.

---

### `ivory scaffold:ci`

Generate CI templates wired to Ivory for GitHub Actions or GitLab CI.

```bash
ivory scaffold:ci              # GitHub by default
ivory scaffold:ci --target gitlab
ivory scaffold:ci --target both --force
```

Creates `.github/workflows/ivory-ci.yml` and/or `.gitlab-ci.yml` running `dotnet build` plus a Ivory doctor + PHP version check.

---

### `ivory scaffold:docker`

Generate a Dockerfile and docker-compose.yml using the resolved PHP version.

```bash
ivory scaffold:docker
```

Produces a simple `Dockerfile` (php:<version>-cli) and `docker-compose.yml` exposing port 8000; extend to install needed PHP extensions.

---

### `ivory completion`

Generate a shell completion script (bash, zsh, fish, PowerShell) that includes commands, ivory.json script names, and PHP versions/aliases from your manifest.

```bash
ivory completion bash     # bash
ivory completion zsh      # zsh
ivory completion fish     # fish
ivory completion powershell
```

Tip: `source <(ivory completion bash)` or add to your shell init file.

---

### `ivory doctor`

Inspect environment and show what Ivory sees:

```bash
ivory doctor
```

Typical output includes:

* Current platform (`PlatformId`)
* Home directory and `~/.ivory` layout (versions, cache, manifest path)
* Installed PHP versions
* Effective project / global PHP version
* Locations of `composer.phar` and system `composer`
* Whether `ivory.json` was found and where
* Whether per-project isolation is enabled for this directory (and paths to `.ivory/php/php.ini` and `conf.d` if present)

This uses:

* `IEnvironmentProbe` to find system `php`/`composer`
* `IPhpInstaller` to inspect installed versions
* `IComposerService` to locate Composer
* `IProjectConfigProvider` to find `ivory.json`

---

## Data & directories

Ivory stores its own data under:

```text
~/.ivory/
    php-versions.json   # PhpVersionsManifest (download sources & checksums)
    versions/           # Installed PHP runtimes, per platform/version
    cache/php/          # Download cache for PHP archives
    config.json         # Global config (e.g. default PHP version)
```

Per-project data:

* `ivory.json` – project configuration (PHP + scripts).
* `.ivory.php-version` – per-project PHP version pin.

