using System.CommandLine;
using Ivory.Application.Deploy;
using Ivory.Cli.Deploy;
using Ivory.Cli.Execution;
using Ivory.Cli.Exceptions;
using Ivory.Cli.Formatting;

namespace Ivory.Cli.Commands;

internal static class ProjectsCommand
{
    public static Command Create(IDeployApiClient apiClient, IDeployConfigStore configStore)
    {
        var orgOption = new Option<Guid>("--org-id")
        {
            Description = "Org id.",
        };

        var list = new Command("list", "List projects in an org.")
        {
            orgOption
        };

        list.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("projects:list", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(configStore, parseResult.GetValue(CommonOptions.ApiUrl), parseResult.GetValue(CommonOptions.UserId)).ConfigureAwait(false);
                var orgId = await ResolveOrgAsync(apiClient, session, parseResult.GetValue(orgOption)).ConfigureAwait(false);

                var projects = await apiClient.GetProjectsAsync(session, orgId).ConfigureAwait(false);
                if (projects.Count == 0)
                {
                    CliConsole.Info("No projects found.");
                    return;
                }

                foreach (var p in projects)
                {
                    Console.WriteLine($"- {p.Name} ({p.Id}) org={p.OrgId}");
                }
            }).ConfigureAwait(false);
        });

        var nameOption = new Option<string>("--name")
        {
            Description = "Project name."
        };
        var create = new Command("create", "Create a project in an org.")
        {
            orgOption,
            nameOption
        };

        create.SetAction(async parseResult =>
        {
            await CommandExecutor.RunAsync("projects:create", async _ =>
            {
                var session = await DeploySessionResolver.ResolveAsync(configStore, parseResult.GetValue(CommonOptions.ApiUrl), parseResult.GetValue(CommonOptions.UserId)).ConfigureAwait(false);
                var orgId = await ResolveOrgAsync(apiClient, session, parseResult.GetValue(orgOption)).ConfigureAwait(false);
                var name = parseResult.GetValue(nameOption) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name)) throw new IvoryCliException("Project name is required.");

                var project = await apiClient.CreateProjectAsync(session, orgId, name).ConfigureAwait(false);
                CliConsole.Success($"Project created: {project.Name} ({project.Id})");
            }).ConfigureAwait(false);
        });

        var command = new Command("projects", "Manage projects.");
        command.Options.Add(CommonOptions.ApiUrl);
        command.Options.Add(CommonOptions.UserId);
        command.Subcommands.Add(list);
        command.Subcommands.Add(create);
        return command;
    }

    private static async Task<Guid> ResolveOrgAsync(IDeployApiClient apiClient, DeploySession session, Guid provided)
    {
        if (provided != Guid.Empty)
        {
            return provided;
        }

        var orgs = await apiClient.GetOrgsAsync(session).ConfigureAwait(false);
        if (orgs.Count == 0)
        {
            throw new IvoryCliException("No orgs found. Create one first with 'iv orgs create --name <name>'.");
        }

        if (orgs.Count > 1)
        {
            var choices = string.Join(", ", orgs.Select(o => $"{o.OrgName} ({o.OrgId})"));
            throw new IvoryCliException($"Org id is required when you belong to multiple orgs. Available: {choices}");
        }

        return orgs[0].OrgId;
    }
}
