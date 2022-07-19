using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyJetTools.Telemetry.Processors;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyJetTools.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddMyTelemetry(this IServiceCollection services,
            string appNamePrefix,
            string appName,
            string zipkinEndpoint = null,
            Func<HttpRequest, bool> httpRequestFilter = null,
            IEnumerable<string> sources = null,
            bool errorStatusOnException = false,
            bool setDbStatementForText = true)
        {
            services.AddOpenTelemetryTracing((builder) =>
                {
                    builder
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.Filter = context =>
                            {
                                if (httpRequestFilter != null && httpRequestFilter(context.Request)) return false;
                                if (context.Request.Path.ToString().Contains("isalive")) return false;
                                if (context.Request.Path.ToString().Contains("metrics")) return false;
                                if (context.Request.Path.ToString().Contains("dependencies")) return false;
                                if (context.Request.Path.ToString().Contains("swagger")) return false;
                                if (context.Request.Path.ToString() == "/") return false;
                                return true;
                            };
                            options.EnableGrpcAspNetCoreSupport = true;
                        })
                        .AddEntityFrameworkCoreInstrumentation(option =>
                        {
                            option.SetDbStatementForText = setDbStatementForText;
                        })
                        .SetSampler(new AlwaysOnSampler())
                        .AddSource(appName)
                        .AddGrpcClientInstrumentation()
                        .AddProcessor(new TelemetryExceptionProcessor())
                        .AddProcessor(new TelemetrySpanProcessor())
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appNamePrefix}{appName}"));

                    if (errorStatusOnException)
                    {
                        builder.SetErrorStatusOnException();
                    }

                    if (sources != null)
                    {
                        foreach (var source in sources)
                        {
                            builder.AddSource(source);
                        }
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
}