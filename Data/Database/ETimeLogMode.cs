namespace QuickTrack.Data.Database;

public enum ETimeLogMode
{
    /// <summary>
    /// Default time mode. Indicates normal, counted work.
    /// </summary>
    Normal,
    /// <summary>
    /// Time mode for break. Indicates a non-counted piece of time.
    /// </summary>
    Break,
    /// <summary>
    /// Day termination. Used to indicate an end.
    /// </summary>
    Quit,
    /// <summary>
    /// Specialized time mode to denote that automated export occured during this time frame.
    /// </summary>
    Export,
    /// <summary>
    /// Similar to <see cref="Break"/> but the time is counted.
    /// </summary>
    /// <remarks>
    /// Supposed to be used when eg. Half-Day off is needed.
    /// </remarks>
    OffTime,
}