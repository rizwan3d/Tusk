using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Tusk.Application.Php;
using Tusk.Application.Scaffolding;
using Tusk.Cli.Helpers;
using Tusk.Domain.Config;

namespace Tusk.Cli.Commands;

internal static class InitCommand
{
    public static Command Create(IPhpVersionResolver resolver, Option<string> phpVersionOption, IPublicIndexScaffolder publicIndexScaffolder)
    {
        var frameworkOption = new Option<FrameworkKind>("--framework")
        {
            Description = "Framework preset to use when generating tusk.json (Generic, Laravel, Symfony).",
            Arity = ArgumentArity.ZeroOrOne
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Overwrite existing tusk.json if it already exists."
        };

        var command = new Command("init", "Create a starter tusk.json for this project.")
        {
            frameworkOption,
            forceOption
        };

        command.SetAction(async parseResult =>
        {
            FrameworkKind framework = parseResult.GetValue(frameworkOption);
            if (!Enum.IsDefined(framework))
            {
                framework = FrameworkKind.Generic;
            }

            bool force = parseResult.GetValue(forceOption);

            string cwd = Environment.CurrentDirectory;
            string tuskPath = Path.Combine(cwd, "tusk.json");

            if (File.Exists(tuskPath) && !force)
            {
                await Console.Error.WriteLineAsync(
                    $"[tusk] tusk.json already exists at {tuskPath}. Use --force to overwrite.").ConfigureAwait(false);
                return;
            }

            var config = TuskConfigFactory.CreateFor(framework);

            string phpVersionSpec = parseResult.GetValue(phpVersionOption) ?? string.Empty;
            var detectedVersion = await VersionHelpers.DetectPhpVersionFromPhpVAsync(resolver, phpVersionSpec).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(detectedVersion) && config is not null)
            {
                config.Php.Version = detectedVersion;
            }

            string composerPath = Path.Combine(cwd, "composer.json");
            if (File.Exists(composerPath))
            {
                try
                {
                    using var composerDoc = JsonDocument.Parse(await File.ReadAllTextAsync(composerPath).ConfigureAwait(false));
                    var root = composerDoc.RootElement;

                    using var stream = new MemoryStream();
                    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                    writer.WriteStartObject();

                    bool hasExtra = false;
                    JsonElement extraElement = default;

                    if (root.TryGetProperty("extra", out var extraElem) &&
                        extraElem.ValueKind == JsonValueKind.Object)
                    {
                        hasExtra = true;
                        extraElement = extraElem;
                    }

                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("extra") && hasExtra)
                        {
                            writer.WritePropertyName("extra");
                            writer.WriteStartObject();

                            foreach (var extraProp in extraElement.EnumerateObject())
                            {
                                if (extraProp.NameEquals("tusk"))
                                    continue;

                                writer.WritePropertyName(extraProp.Name);
                                extraProp.Value.WriteTo(writer);
                            }

                            writer.WritePropertyName("tusk");
                            if (config is not null)
                            {
                                TuskConfigSerialization.WriteTuskConfigObject(writer, config);
                            }
                            else
                            {
                                writer.WriteStartObject();
                                writer.WriteEndObject();
                            }

                            writer.WriteEndObject();
                        }
                        else
                        {
                            writer.WritePropertyName(prop.Name);
                            prop.Value.WriteTo(writer);
                        }
                    }

                    if (!hasExtra)
                    {
                        writer.WritePropertyName("extra");
                        writer.WriteStartObject();
                        writer.WritePropertyName("tusk");
                        if (config is not null)
                        {
                            TuskConfigSerialization.WriteTuskConfigObject(writer, config);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    await writer.FlushAsync().ConfigureAwait(false);

                    await File.WriteAllBytesAsync(tuskPath, stream.ToArray()).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    await Console.Error.WriteLineAsync($"[tusk] Failed to base tusk.json on composer.json: {ex.Message}").ConfigureAwait(false);
                    if (config is not null)
                    {
                        var json = TuskConfigSerialization.SerializeTuskConfig(config);
                        await File.WriteAllTextAsync(tuskPath, json).ConfigureAwait(false);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    await Console.Error.WriteLineAsync($"[tusk] Failed to base tusk.json on composer.json: {ex.Message}").ConfigureAwait(false);
                    if (config is not null)
                    {
                        var json = TuskConfigSerialization.SerializeTuskConfig(config);
                        await File.WriteAllTextAsync(tuskPath, json).ConfigureAwait(false);
                    }
                }
                catch (JsonException ex)
                {
                    await Console.Error.WriteLineAsync($"[tusk] Failed to base tusk.json on composer.json: {ex.Message}").ConfigureAwait(false);
                    if (config is not null)
                    {
                        var json = TuskConfigSerialization.SerializeTuskConfig(config);
                        await File.WriteAllTextAsync(tuskPath, json).ConfigureAwait(false);
                    }
                }

                string composerLock = Path.Combine(cwd, "composer.lock");
                string tuskLock = Path.Combine(cwd, "tusk.lock");
                if (File.Exists(composerLock) && (!File.Exists(tuskLock) || force))
                {
                    File.Copy(composerLock, tuskLock, overwrite: true);
                }
            }
            else
            {
                if (config is not null)
                {
                    var json = TuskConfigSerialization.SerializeTuskConfig(config);
                    await File.WriteAllTextAsync(tuskPath, json).ConfigureAwait(false);
                }
            }

            publicIndexScaffolder.EnsureDefaultPublicIndex(cwd);

            Console.WriteLine($"[tusk] Created tusk.json for framework '{framework}' in {cwd}");
        });

        return command;
    }
}
