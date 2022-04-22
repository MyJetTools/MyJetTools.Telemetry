using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry.Trace;
using Status = OpenTelemetry.Trace.Status;

namespace MyJetTools.Telemetry;

public static class TelemetrySource
{
    public static ActivitySource Source { get; set; }

    static TelemetrySource()
    {
        Source = new ActivitySource("DefaultName");

        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
        });
    }
    
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(name, kind);
    }

    public static Activity? FailActivity(this Exception ex)
    {
        var activity = Activity.Current;
            
        if (activity == null) return activity;
            
        activity.RecordException(ex);
        activity.SetStatus(Status.Error);

        return activity;
    }

    public static Activity? WriteToActivity(this Exception ex)
    {
        var activity = Activity.Current;

        if (activity == null) return activity;

        activity.RecordException(ex);
            

        return activity;
    }

    public static Activity? AddToActivityAsJsonTag(this object obj, string tag)
    {
        var activity = Activity.Current;
        activity?.AddTag(tag, JsonSerializer.Serialize(obj));
        return activity;
    }

    public static Activity? AddToActivityAsTag(this object obj, string tag)
    {
        var activity = Activity.Current;
        activity?.AddTag(tag, obj);
        return activity;
    }
}