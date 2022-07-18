using System.Collections.Immutable;
using Fastenshtein;
using QuickTrack.Exporters;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class ExportCommand : IConsoleCommand
{
    private readonly ImmutableArray<ExporterBase> _exporters;

    public ExportCommand(ConfigHost configHost)
    {
        _exporters = typeof(ExportCommand).Assembly
            .GetTypes()
            .Where((type) => type.IsAssignableTo(typeof(ExporterBase)))
            .Where((type) => !type.IsEquivalentTo(typeof(ExporterBase)))
            .Select((type) => type.CreateInstance<ExporterBase>(configHost))
            .ToImmutableArray();
        Keys        = "export".MakeArray();
        Description = "Exports the data of the requested range to the given exporter.";
        Pattern = string.Join(
            "\r\n",
            _exporters
                .Select((exporter) => $"export {exporter.Identifier} {exporter.Pattern}")
                .DefaultIfEmpty("No exporters are available")
                .Prepend("export help EXPORTER"));
    }

    public string[] Keys { get; }
    public string Description { get; }
    public string Pattern { get; }

    public void Execute(string[] args)
    {
        if (args.Length is 0)
            throw new ArgumentException("args requires at least one argument");
        var identifier = args.First();
        var exporter = _exporters.FirstOrDefault(
            (q) => string.Equals(
                q.Identifier,
                identifier,
                StringComparison.InvariantCultureIgnoreCase));
        if (identifier is "help")
        {
            ShowHelp(args.Skip(1).ToArray());
            return;
        }

        if (exporter is null)
        {
            CouldBeMistake(identifier);
            return;
        }

        exporter.Export(args.Skip(1).ToArray());
    }

    private void ShowHelp(string[] args)
    {
        if (args.Length is 0)
        {
            new ConsoleString
            {
                Text       = $"No exporter specified. Available exporters:",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            foreach (var it in _exporters)
            {
                new ConsoleString
                {
                    Text       = $"- {it.Identifier}",
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }

            return;
        }

        var exporterIdentifier = args.First();
        var exporter = _exporters.FirstOrDefault(
            (q) => string.Equals(
                q.Identifier,
                exporterIdentifier,
                StringComparison.InvariantCultureIgnoreCase));

        if (exporter is null)
        {
            CouldBeMistake(exporterIdentifier);
            return;
        }

        new ConsoleString
        {
            Text       = exporter.HelpText,
            Foreground = ConsoleColor.Cyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }

    private void CouldBeMistake(string identifierCandidate)
    {
        var candidates = _exporters
            .Select((q) => (q.Identifier, dst: Levenshtein.Distance(q.Identifier, identifierCandidate)))
            .Append(("help", dst: Levenshtein.Distance("help", identifierCandidate)))
            .Where((q) => q.dst < 3)
            .ToArray();
        if (!candidates.Any())
        {
            new ConsoleString($"No exporter with the name '{identifierCandidate}' could be found.")
                    {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                .WriteLine();
            return;
        }

        new ConsoleString("Did you mean:")
                {Foreground = ConsoleColor.DarkYellow, Background = ConsoleColor.Black}
            .WriteLine();
        foreach (var (candidate, _) in candidates.OrderBy((q) => q.dst))
        {
            new ConsoleString($"- {candidate}")
                    {Foreground = ConsoleColor.DarkYellow, Background = ConsoleColor.Black}
                .WriteLine();
        }
    }
}