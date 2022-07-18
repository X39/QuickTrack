using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using X39.Util;
using X39.Util.Collections;
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

    protected override void DoExport(IEnumerable<TimeLogFile> timeLogFiles, string[] args)
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
                outputFile = args[1];
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
        ExportTransformed(timeLogFiles, template, outputFile);
        new ConsoleString
        {
            Text       = $"Export created at '{outputFile}'",
            Foreground = ConsoleColor.Black,
            Background = ConsoleColor.White,
        }.WriteLine();
    }

    private static void ExportTransformed(IEnumerable<TimeLogFile> timeLogFiles, Template template, string outputFile)
    {
        using var writer = new StreamWriter(outputFile, false);
        if (template.PrefixWith is not null)
            writer.Write(template.PrefixWith);
        foreach (var (timeLogLine, index) in timeLogFiles.SelectMany((q) => q.GetLines()).Indexed())
        {
            if (index > 0)
                writer.Write(template.SeparateBy);
            var dateString = timeLogLine.TimeStampStart.ToString(template.DateFormat);
            var startString = timeLogLine.TimeStampStart.ToString(template.TimeStampStartFormat);
            var endString = timeLogLine.TimeStampEnd.ToString(template.TimeStampEndFormat);
            var allStrings = new[]
            {
                dateString,
                startString,
                endString,
                timeLogLine.Project,
                timeLogLine.Message
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
            writer.Write(replaced);
        }
        if (template.SuffixWith is not null)
            writer.Write(template.SuffixWith);
    }

    private static Template ParseTransformTemplateFromFile(string transformFile)
    {
        var configHost = new ConfigHost(transformFile);
        return configHost.Bind<Template>(null);
    }

    public TemplateExport(ConfigHost configHost) : base(configHost)
    {
    }
}