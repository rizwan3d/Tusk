using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Ivory.Domain.Cli.Doctor;

namespace Ivory.Cli.Formatting;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(DoctorModel))]
[JsonSerializable(typeof(AvailableRequest))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}

