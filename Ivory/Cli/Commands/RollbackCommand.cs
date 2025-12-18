using System.CommandLine;
using Ivory.Application.Deploy;
using Ivory.Cli.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class RollbackCommand
{
    public static Command Create(IDeployApiClient apiClient, IDeployConfigStore configStore)
    {
        var projectOption = new Option<Guid>("--project-id")
        {
            Description = "Project id to rollback."
        };

        var targetOption = new Option<Guid>("--target-deployment-id")
        {
            Description = "Deployment id to rollback to."
        };

        var apiUrlOption = new Option<string>("--api-url")
        {
            Description = "Override API base URL for this command."
        };

        var userIdOption = new Option<string>("--user-id")
        {
            Description = "Override user id for this command."
        };

        var command = new Command("rollback", "Create a rollback deployment that targets a previous deployment.")
        {
            projectOption,
            targetOption,
            apiUrlOption,
            userIdOption
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("rollback", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(
                    configStore,
                    parseResult.GetValue(apiUrlOption),
                    parseResult.GetValue(userIdOption)).ConfigureAwait(false);

                var projectId = parseResult.GetValue(projectOption);
                var targetId = parseResult.GetValue(targetOption);

                if (projectId == Guid.Empty || targetId == Guid.Empty)
                {
                    throw new IvoryCliException("Both --project-id and --target-deployment-id are required.");
                }

                var result = await apiClient.RollbackAsync(session, projectId, targetId).ConfigureAwait(false);
                CliConsole.Success($"Rollback deployment created: {result.Id}");
            }).ConfigureAwait(false);
        });

        return command;
    }
}
