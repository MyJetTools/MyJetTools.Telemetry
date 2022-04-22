using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MyJetTools.Telemetry.Processors;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyJetTools.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection SetupsTelemetry(this IServiceCollection services, TelemetryConfiguration config,
        string? zipkinEndpoint = null)
    {
        services.AddOpenTelemetryTracing((builder) =>
        {
            builder
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = config.ValidateRoutes;
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

            TelemetrySource.Source = new ActivitySource(config.AppName);

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
}