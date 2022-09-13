namespace QuickTrack;

public static class Constants
{
    public const string DateFormatFormal = "dd.MM";
    public const string DateFormatNoDot  = "ddMM";

    public const string MessageForQuit      = "quit";
    public const string ProjectForQuit      = "quit";
    public const string ProjectForBreak     = "break";
    public const string MessageForBreak     = "pause";
    public const string DatabaseFile        = "QuickTrack.db";
    public const string ProjectForSapExport = "SAP-BBD";

    public static class ErrorCodes
    {
        public const int Ok                     = 0;
        public const int DatabaseFailedToCreate = -2;
    }
}