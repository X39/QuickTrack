using System.Collections.Immutable;
using System.Text;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class SearchProjectCommand : SearchCommand
{
    public override string[] Keys { get; } = {"search-project"};

    public override string Description =>
        "Searches the given number of log-files for the given project. " +
        "If no argument is provided, only today and yesterday will be listed. " +
        "If * is provided, all log-files will be searched. " +
        "If week is provided, the previous 7 days will be listed. " +
        "If month is provided, the previous 31 days will be listed. " +
        "If a number is provided, the previous X days will be listed where X is the number and 1 is today.";

    // ReSharper disable once StringLiteralTypo
    public override string Pattern => "search-project [ NUMBEROFDAYS | week | month ] TEXT...";

    public override async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
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
        var separators = new[]
        {
            ' ',
            '[',
            ']',
            '|',
            ':'
        };
        var (days, text) = ParseArguments(args);
        await PrintDaysFoundAsync(days, [], [], [project], separators, text, cancellationToken);
    }
}