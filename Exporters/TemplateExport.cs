using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Exporters;

public class TemplateExport : ExporterBase
{
    public class Template
    {
        private string? _timeStampEndFormat;
        private string? _timeStampStartFormat;

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string DateFormat { get; set; } = "dd.MM.yyyy";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string TimeFormat { get; set; } = "hh:mm:ss";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string TimeStampStartFormat
        {
            get => _timeStampStartFormat ?? TimeFormat;
            set => _timeStampStartFormat = value.IsNullOrWhiteSpace() ? null : value;
        }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string TimeStampEndFormat
        {
            get => _timeStampEndFormat ?? TimeFormat;
            set => _timeStampEndFormat = value.IsNullOrWhiteSpace() ? null : value;
        }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? PrefixWith { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string? SuffixWith { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string SeparateBy { get; set; } = "\r\n";


        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ProjectReferenceName { get; set; } = "project";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string MessageReferenceName { get; set; } = "message";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string DateReferenceName { get; set; } = "date";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string StartReferenceName { get; set; } = "start";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string EndReferenceName { get; set; } = "end";

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ReplaceRegex { get; set; } = "${date};${start};${end};${project};${message}";
    }

    public override string Identifier => "template";

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    protected override string ArgsPattern => "TEMPLATE OUTPUT";

    private const string TemplateFolderName = "templates";

    public override string HelpText => $"Creates a transforming export using a template (arg 'TEMPLATE') that " +
                                       $"is located inside of the '{TemplateFolderName}', " +
                                       $"creating an export file (arg 'OUTPUT') at the current workspace. " +
                                       $"Template files have the following format:\r\n" +
                                       ConfigHost.BindingTemplate<Template>(null);

    protected override async ValueTask DoExportAsync(
        IEnumerable<Day> days,
        string[] args,
        CancellationToken cancellationToken)
    {
        string transformFile, outputFile;
        switch (args.Length)
        {
            case 0:
                new ConsoleString
                {
                    Text       = $"No values provided for template and output files",
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                return;
            case 1:
                new ConsoleString
                {
                    Text       = $"No value provided for output file",
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                return;
            case 2:
                transformFile = args[0];
                outputFile    = args[1];
                break;
            default:
                new ConsoleString
                {
                    Text       = $"Too many arguments provided",
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                return;
        }

        if (transformFile.IndexOfAny(Path.GetInvalidFileNameChars()) is not -1)
        {
            new ConsoleString
            {
                Text       = $"Transform filename contains invalid characters",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        transformFile = Path.Combine(Programm.Workspace, TemplateFolderName, transformFile);
        if (!File.Exists(transformFile))
        {
            new ConsoleString
            {
                Text       = $"File '{transformFile}' does not exist",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        var template = ParseTransformTemplateFromFile(transformFile);
        outputFile = MakeOutputFolderPath(outputFile);
        await ExportTransformedAsync(days, template, outputFile, cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString
        {
            Text       = $"Export created at '{outputFile}'",
            Foreground = ConsoleColor.Black,
            Background = ConsoleColor.White,
        }.WriteLine();
    }

    private static async Task<Project> GetProjectAsync(
        IDictionary<int, Project> dictionary,
        int projectId,
        CancellationToken cancellationToken)
    {
        if (dictionary.TryGetValue(projectId, out var project))
            return project;
        project               = await QtRepository.GetProjectAsync(projectId, cancellationToken);
        dictionary[projectId] = project;
        return project;
    }

    private static async Task ExportTransformedAsync(
        IEnumerable<Day> days,
        Template template,
        string outputFile,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputFile, false);
        if (template.PrefixWith is not null)
            await writer.WriteAsync(template.PrefixWith);
        var index = 0;
        var projectDictionary = new Dictionary<int, Project>();
        foreach (var day in days)
        {
            TimeLog? previous = null;
            await foreach (var timeLog in day
                               .GetTimeLogs(cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (previous is not null)
                {
                    await WriteAsync(
                        projectDictionary,
                        template,
                        index,
                        writer,
                        previous,
                        timeLog.TimeStamp - previous.TimeStamp,
                        cancellationToken);
                    index++;
                }

                previous = timeLog;
            }

            // ReSharper disable once InvertIf
            if (previous is not null)
            {
                await WriteAsync(
                    projectDictionary,
                    template,
                    index,
                    writer,
                    previous,
                    TimeSpan.Zero,
                    cancellationToken);
                index++;
            }
        }

        if (template.SuffixWith is not null)
            await writer.WriteAsync(template.SuffixWith);
    }

    private static async Task WriteAsync(
        Dictionary<int, Project> projectDictionary,
        Template template,
        int index,
        TextWriter writer,
        TimeLog timeLog,
        TimeSpan timeRequired,
        CancellationToken cancellationToken)
    {
        if (index > 0)
            await writer.WriteAsync(template.SeparateBy);
        var dateString = timeLog.TimeStamp.ToString(template.DateFormat);
        var startString = timeLog.TimeStamp.ToString(template.TimeStampStartFormat);
        var endString = (timeLog.TimeStamp + timeRequired).ToString(template.TimeStampEndFormat);
        var project = await GetProjectAsync(projectDictionary, timeLog.ProjectFk, cancellationToken)
            .ConfigureAwait(false);
        var allStrings = new[]
        {
            dateString,
            startString,
            endString,
            project.Title,
            timeLog.Message
        };
        var refNames = new[]
        {
            template.DateReferenceName,
            template.StartReferenceName,
            template.EndReferenceName,
            template.ProjectReferenceName,
            template.MessageReferenceName
        };
        var regexInput = string.Concat(allStrings);
        var regexPattern = string.Concat(allStrings.Select((s, i) => $"(?<{refNames[i]}>.{{{s.Length}}})"));
        var replaced = Regex.Replace(regexInput, regexPattern, template.ReplaceRegex);
        await writer.WriteAsync(replaced);
    }

    private static Template ParseTransformTemplateFromFile(string transformFile)
    {
        var configHost = new ConfigHost(transformFile);
        return configHost.Bind<Template>(null);
    }
}