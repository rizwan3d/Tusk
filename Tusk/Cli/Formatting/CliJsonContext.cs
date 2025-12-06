using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Tusk.Domain.Cli.Doctor;

namespace Tusk.Cli.Formatting;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(DoctorModel))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}
