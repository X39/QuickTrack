using System.Collections.Immutable;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class ProjectCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"project"};

    public string Description => "Allows to list, change, show or set the current project.";

    public string Pattern => "project [ list | set | edit ]";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var arg = args.FirstOrDefault();
        switch (arg)
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
}