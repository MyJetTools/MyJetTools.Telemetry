namespace MyJetTools.Telemetry;

public class TelemetryConfiguration
{
    public string AppName { get; }
    public string AppNamePrefix { get; }
    public bool IsShowErrorStatus { get; }
    public IEnumerable<string>? IgnoreRoutes { get; }
    public IEnumerable<string> Sources { get; }

    public TelemetryConfiguration(string appName, string appNamePrefix, bool isShowErrorStatus,
        IEnumerable<string>? ignoreRoutes, IEnumerable<string> sources)
    {
        AppName = appName;
        AppNamePrefix = appNamePrefix;
        IsShowErrorStatus = isShowErrorStatus;
        IgnoreRoutes = ignoreRoutes;
        Sources = sources;
    }


    public bool ValidateRoutes(string path)
    {
        if (IgnoreRoutes == null || !IgnoreRoutes.Any())
            return false;

        return !IgnoreRoutes.Any(path.Contains);
    }
}