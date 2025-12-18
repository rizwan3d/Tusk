using System.CommandLine;
using Ivory.Application.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class RegisterCommand
{
    public static Command Create(IDeployApiClient apiClient)
    {
        var apiUrlOption = new Option<string>("--api-url")
        {
            Description = "Base URL of the deploy API."
        };

        var emailOption = new Option<string>("--email")
        {
            Description = "Email for the new user."
        };

        var passwordOption = new Option<string>("--password")
        {
            Description = "Password for the new user."
        };

        var command = new Command("register", "Register a new user in the deploy API.")
        {
            apiUrlOption,
            emailOption,
            passwordOption
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("register", async _ =>
            {
                var apiUrl = parseResult.GetValue(apiUrlOption);
                var email = parseResult.GetValue(emailOption) ?? string.Empty;
                var password = parseResult.GetValue(passwordOption) ?? string.Empty;

                var result = await apiClient.RegisterUserAsync(
                    apiUrl ?? string.Empty,
                    email,
                    password).ConfigureAwait(false);

                CliConsole.Success($"User registered: {result.Email} ({result.Id})");
            }).ConfigureAwait(false);
        });

        return command;
    }
}
