using System.CommandLine;
using Ivory.Application.Deploy;
using Ivory.Cli.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class DomainsCommand
{
    public static Command Create(IDeployApiClient apiClient, IDeployConfigStore configStore)
    {
        var projectOption = new Option<Guid>("--project-id")
        {
            Description = "Project id to list domains for."
        };

        var apiUrlOption = new Option<string>("--api-url")
        {
            Description = "Override API base URL for this command."
        };

        var userIdOption = new Option<string>("--user-id")
        {
            Description = "Override user id for this command."
        };

        var command = new Command("domains", "List domains bound to a project.")
        {
            projectOption,
            apiUrlOption,
            userIdOption
        };

        command.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("domains", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(
                    configStore,
                    parseResult.GetValue(apiUrlOption),
                    parseResult.GetValue(userIdOption)).ConfigureAwait(false);

                var projectId = parseResult.GetValue(projectOption);
                if (projectId == Guid.Empty)
                {
                    throw new IvoryCliException("Project id is required.");
                }

                var domains = await apiClient.GetDomainsAsync(session, projectId).ConfigureAwait(false);

                if (domains.Count == 0)
                {
                    CliConsole.Info("No domains found.");
                    return;
                }

                CliConsole.Success($"Domains for project {projectId}:");
                foreach (var domain in domains)
                {
                    var flags = new List<string>();
                    if (domain.IsWildcard) flags.Add("wildcard");
                    if (domain.ManagedCertificate) flags.Add("managed-cert");

                    var suffix = flags.Count > 0 ? $" ({string.Join(", ", flags)})" : string.Empty;
                    Console.WriteLine($"- {domain.Hostname}{suffix}");
                }
            }).ConfigureAwait(false);
        });

        return command;
    }
}
