using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MyJetTools.Telemetry.Processors;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyJetTools.Telemetry;

public static class TelemetryExtensions
{
    public static ActivitySource Source;
    
    public static IServiceCollection SetupsTelemetry(this IServiceCollection services, TelemetryConfiguration config,
        string? zipkinEndpoint = null, bool isAddConsoleExporter = false)
    {
        Source = new ActivitySource(config.AppName);

        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                ActivitySamplingResult.AllData
        });
        
        services.AddOpenTelemetryTracing((builder) =>
        {
            builder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = itm => config.ValidateRoutes(itm.Request.Path.ToString());
                    options.EnableGrpcAspNetCoreSupport = true;
                })
                .SetSampler(new AlwaysOnSampler())
                .AddProcessor(new TelemetryExceptionProcessor())
                .AddProcessor(new TelemetrySpanProcessor())
                .AddGrpcClientInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService($"{config.AppNamePrefix}{config.AppName}"));

            foreach (var source in config.Sources)
            {
                builder.AddSource(source);
            }

            if (config.IsShowErrorStatus)
            {
                builder.SetErrorStatusOnException();
            }

            if (!string.IsNullOrEmpty(zipkinEndpoint))
            {
                builder.AddZipkinExporter(options =>
                {
                    options.Endpoint = new Uri(zipkinEndpoint);
                    options.UseShortTraceIds = true;
                    options.ExportProcessorType = ExportProcessorType.Batch;
                });

                Console.WriteLine("Telemetry to Zipkin - ACTIVE");
            }
            else
            {
                Console.WriteLine("Telemetry to Zipkin - DISABLED");
            }

            if (isAddConsoleExporter)
            {
                Console.WriteLine("Added console exporter for telemetry");
                builder.AddConsoleExporter();
            }
        });

        return services;
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