using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyJetTools.Telemetry.Processors;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyJetTools.Telemetry;

public static class TelemetryExtensions
{
    public static ActivitySource Source;

    public static IServiceCollection SetupsTelemetry(this IServiceCollection services,
        string appName,
        string appNamePrefix,
        string zipkinEndpoint = null,
        Func<HttpRequest, bool> httpRequestFilter = null,
        IEnumerable<string> sources = null,
        bool errorStatusOnException = false)
    {
        Source = new ActivitySource(appName);

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
                    options.Filter = context =>
                    {
                        if (context.Request.Path.ToString().Contains("isalive")) return false;
                        if (context.Request.Path.ToString().Contains("metrics")) return false;
                        if (context.Request.Path.ToString().Contains("dependencies")) return false;
                        if (context.Request.Path.ToString().Contains("swagger")) return false;
                        return true;
                    };
                    options.EnableGrpcAspNetCoreSupport = true;
                })
                .SetSampler(new AlwaysOnSampler())
                .AddProcessor(new TelemetryExceptionProcessor())
                .AddProcessor(new SpanTraceProcessor())
                .AddGrpcClientInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService($"{appNamePrefix}{appName}"));


            if (sources != null)
            {
                foreach (var source in sources)
                {
                    builder.AddSource(source);
                }
            }


            if (errorStatusOnException)
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