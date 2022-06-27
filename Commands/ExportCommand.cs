using System.Collections.Immutable;
using Fastenshtein;
using QuickTrack.Exporters;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class ExportCommand : ICommand
{
    private readonly ImmutableArray<ExporterBase> _exporters;

    public ExportCommand()
    {
        _exporters = typeof(ExportCommand).Assembly
            .GetTypes()
            .Where((type) => type.IsAssignableTo(typeof(ExporterBase)))
            .Select((type) => type.CreateInstance<ExporterBase>())
            .ToImmutableArray();
        Keys        = "export".MakeArray();
        Description = "Exports the data of the requested range to the given exporter.";
        Pattern = string.Join(
            "\r\n",
            _exporters.Select((exporter) => $"export {exporter.Identifier} FROM TO {exporter.Pattern}"));
    }

    public string[] Keys { get; }
    public string Description { get; }
    public string Pattern { get; }

    public void Execute(string[] args)
    {
        if (args.Length is 0)
            throw new ArgumentException("args requires at least one argument");
        var identifier = args.First();
        var exporter = _exporters.FirstOrDefault((q) => string.Equals(
            q.Identifier,
            identifier,
            StringComparison.InvariantCultureIgnoreCase));
        if (exporter is null)
        {
            CouldBeMistake(identifier);
            return;
        }
        exporter.Export(args.Skip(1).ToArray());
    }
    private void CouldBeMistake(string identifierCandidate)
    {
        var candidates = _exporters
            .Select((q) => (q.Identifier, dst: Levenshtein.Distance(q.Identifier, identifierCandidate)))
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