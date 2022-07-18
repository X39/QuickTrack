namespace QuickTrack.Commands;

public class ListCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"list"};

    public string Description =>
        "Lists the logged entries. " +
        "If no argument is provided, only today and yesterday will be listed. " +
        "If week is provided, the previous 7 days will be listed. " +
        "If month is provided, the previous 31 days will be listed. " +
        "If a number is provided, the previous X days will be listed where X is the number and 1 is today.";

    // ReSharper disable once StringLiteralTypo
    public string Pattern => "list [ NUMBEROFDAYS | week | month ]";

    public void Execute(string[] args)
    {
        var first = args.FirstOrDefault()?.ToLower().Trim();
        var days = first switch
        {
            "week"                           => 7,
            "month"                          => 31,
            { } when first.All(char.IsDigit) => int.Parse(first),
            _                                => 1,
        } - 1;
        var logFiles = Programm.LoadLogFilesFromDisk()
            .Where((q) => (DateTime.Today - q.Date.ToDateTime(TimeOnly.MinValue)).Days <= days)
            .OrderBy((q) => q.Date)
            .ToList();
        foreach (var logFile in logFiles)
        {
            var tag = Programm.GetPrettyPrintTag(logFile);
            Programm.PrintLogFile(logFile, tag);
        }
    }
}