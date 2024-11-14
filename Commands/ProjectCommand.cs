using System.Collections.Immutable;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class ProjectCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"project"};

    public string Description => "Allows to list all projects, edit the name of a project, " +
                                 "show the current project (no arg), merge two projects or set the current project.";

    public string Pattern => "project [ list | set | edit | merge | remove-empty | configured | get-configurations | get-configuration | set-configuration | days ]";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var arg = args.FirstOrDefault();
        switch (arg?.ToLowerInvariant())
        {
            default:
            case null:
                new ConsoleString(Programm.Host.CurrentProject?.Title ?? "<NULL>")
                {
                    Foreground = ConsoleColor.Magenta,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                return;
            case "list":
                await ListProjectsAsync(cancellationToken);
                return;
            case "set":
                await SetProjectAsync(cancellationToken);
                return;
            case "edit":
                await EditProjectAsync(cancellationToken);
                return;
            case "merge":
                await MergeProjectAsync(cancellationToken);
                return;
            case "remove-empty":
                await RemoveEmptyProjectsAsync(cancellationToken);
                return;
            case "configured":
                await ListConfiguredProjectsAsync(cancellationToken);
                return;
            case "get-configurations":
                await ListProjectConfigurationsAsync(cancellationToken);
                return;
            case "get-configuration":
                await GetProjectConfigurationAsync(cancellationToken);
                return;
            case "set-configuration":
                await SetProjectConfigurationAsync(cancellationToken);
                return;
            case "days":
                await ListProjectDaysAsync(cancellationToken);
                return;
        }
    }
    private async Task ListProjectDaysAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
                                         .ConfigureAwait(false);
        new ConsoleString("Please choose a project to list the days of:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var project = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        await foreach (var day in project.GetDays(cancellationToken))
        {
            new ConsoleString(day.Date.Date.ToString("yyyy-MM-dd"))
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
        }
    }
    private async Task SetProjectConfigurationAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose a project to set the configuration of:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var project = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);

        var realmTuples = await project.GetJsonAttachmentRealmsAsync(cancellationToken);
        if (realmTuples.Count == 0)
        {
            new ConsoleString("No configurations found")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }
        new ConsoleString("Please choose a configuration to set:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var tuple = AskConsole.ForValueFromCollection(
            realmTuples,
            (tuple) => new ConsoleString($"{tuple.realm} ({tuple.id})")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        var jsonAttachment = await project.GetJsonAttachmentAsync(tuple.id, cancellationToken);
        new ConsoleString(jsonAttachment.Json)
        {
            Foreground = ConsoleColor.DarkCyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
        new ConsoleString("Please enter the new configuration:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var newJson = Console.ReadLine() ?? string.Empty;
        jsonAttachment.Json = newJson;
        await jsonAttachment.UpdateAsync(cancellationToken);
        new ConsoleString($"Configuration updated to {newJson}")
        {
            Foreground = ConsoleColor.DarkGreen,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }
    private async Task GetProjectConfigurationAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose a project to list the configurations of:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var project = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);

        var realmTuples = await project.GetJsonAttachmentRealmsAsync(cancellationToken);
        if (realmTuples.Count == 0)
        {
            new ConsoleString("No configurations found")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }
        new ConsoleString("Please choose a configuration to retrieve:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var tuple = AskConsole.ForValueFromCollection(
            realmTuples,
            (tuple) => new ConsoleString($"{tuple.realm} ({tuple.id})")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        var jsonAttachment = await project.GetJsonAttachmentAsync(tuple.id, cancellationToken);
        new ConsoleString(jsonAttachment.Json)
        {
            Foreground = ConsoleColor.DarkCyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }
    private async Task ListProjectConfigurationsAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose a project to list the configurations of:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var project = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);

        var realmTuples = await project.GetJsonAttachmentRealmsAsync(cancellationToken);
        if (realmTuples.Count == 0)
        {
            new ConsoleString("No configurations found")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }
        new ConsoleString("Configurations:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        foreach (var (id, realm) in realmTuples)
        {
            new ConsoleString($"- {realm} ({id})")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
        }
    }
    private async Task ListConfiguredProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var project in projects)
        {
            var realmTuples = await project.GetJsonAttachmentRealmsAsync(cancellationToken);
            if (realmTuples.Count == 0)
                continue;
            new ConsoleString($"- {project.Title} ({realmTuples.Count} configurations)")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
        }
    }

    private async Task EditProjectAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose a project to edit:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var project = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);

        string newTitle;
        do
        {
            new ConsoleString("Please enter a new name:")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
            newTitle = Console.ReadLine() ?? string.Empty;
        } while (newTitle.IsNullOrWhiteSpace());

        project.Title = newTitle;
        await project.UpdateAsync(cancellationToken);
        if (Programm.Host.CurrentProject?.Id == project.Id)
            Programm.Host.CurrentProject = project;
    }

    private async Task ListProjectsAsync(CancellationToken cancellationToken)
    {
        new ConsoleString("Available projects:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        await foreach (var project in QtRepository.GetProjects(cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            new ConsoleString($"- {project.Title}")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
        }
    }

    private async Task SetProjectAsync(CancellationToken cancellationToken)
    {
        var project = await Prompt.ForProjectAsync(cancellationToken);
        Programm.Host.CurrentProject = project;
    }

    private async Task MergeProjectAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose the project to merge from (this one will vanish once the merge is complete):")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var projectFrom = AskConsole.ForValueFromCollection(
            projects,
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.DarkRed,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        new ConsoleString("Please choose a project to merge into (this one will stay once the merge is complete):")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var projectInto = AskConsole.ForValueFromCollection(
            projects.Except(projectFrom.MakeEnumerable()),
            (project) => new ConsoleString(project.Title)
            {
                Foreground = ConsoleColor.DarkGreen,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);


        await foreach (var day in projectFrom.GetTimeLogs(cancellationToken)
                           .ConfigureAwait(false))
        {
            day.ProjectFk = projectInto.Id;
            await day.UpdateAsync(this, cancellationToken);
        }

        await projectFrom.DeleteAsync(cancellationToken);
    }

    private async Task RemoveEmptyProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var project in projects)
        {
            var hasTimeLogs = await QtRepository.HasTimeLogsAsync(project);
            if (hasTimeLogs)
                continue;
            await project.DeleteAsync(cancellationToken);
        }
    }
}