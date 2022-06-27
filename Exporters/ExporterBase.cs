namespace QuickTrack.Exporters;

public abstract class ExporterBase
{
    /// <summary>
    /// The identifier of this exporter.
    /// </summary>
    public abstract string Identifier { get; set; }

    /// <summary>
    /// The pattern (minus the export command and identifier),
    /// the arguments of this exporter expect.
    /// </summary>
    public abstract string Pattern { get; set; }

    /// <summary>
    /// Perform the actual export
    /// </summary>
    /// <param name="timeLogFiles">
    /// The log-files to be exported
    /// </param>
    /// <param name="args">
    /// The arguments passed into the exporter
    /// </param>
    protected abstract void DoExport(IEnumerable<TimeLogFile> timeLogFiles, string[] args);

    /// <summary>
    /// Parses the from and to arguments and executes the exporter.
    /// </summary>
    /// <param name="args"></param>
    public void Export(string[] args)
    {
        throw new NotImplementedException();
    }
}