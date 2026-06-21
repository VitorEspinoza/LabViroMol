using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LabViroMol.Modules.Shared.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    private const string ServiceName = "labviromol-api";
    private const string ServiceNamespace = "labviromol";

    private const string OtlpEndpointConfigKey = "OpenTelemetry:OtlpEndpoint";

    private const string OtlpEndpointEnvKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string TracingSamplingRatioConfigKey = "OpenTelemetry:Tracing:SamplingRatio";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environmentName = builder.Environment.EnvironmentName;
        var otlpEndpoint = ResolveOtlpEndpoint(configuration);

        ConfigureLogging(builder, environmentName, otlpEndpoint);

        builder.Services.AddObservabilityTelemetry(configuration, environmentName, otlpEndpoint);

        return builder;
    }

    private static void ConfigureLogging(
        WebApplicationBuilder builder,
        string environmentName,
        string? otlpEndpoint)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(ConfigureResource(ResourceBuilder.CreateDefault(), environmentName));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddProcessor(new PiiRedactionLogProcessor());

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                logging.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    otlpOptions.Endpoint = new Uri(BuildSignalEndpoint(otlpEndpoint, "v1/logs"));
                });
            }
        });
    }

    private static IServiceCollection AddObservabilityTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName,
        string? otlpEndpoint)
    {
        var samplingRatio = ResolveTracingSamplingRatio(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, environmentName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(samplingRatio))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddSource(LabViroMolDiagnostics.Name);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        otlpOptions.Endpoint = new Uri(BuildSignalEndpoint(otlpEndpoint, "v1/traces"));
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(LabViroMolDiagnostics.Name)
                    .AddMeter("Npgsql");

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter((otlpOptions, metricReaderOptions) =>
                    {
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        otlpOptions.Endpoint = new Uri(BuildSignalEndpoint(otlpEndpoint, "v1/metrics"));
                        metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                    });
                }
            });

        return services;
    }

    private static ResourceBuilder ConfigureResource(ResourceBuilder resource, string environmentName) =>
        resource
            .AddService(serviceName: ServiceName, serviceNamespace: ServiceNamespace)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", environmentName),
            });

    private static string BuildSignalEndpoint(string baseEndpoint, string signalPath)
    {
        var trimmedBase = baseEndpoint.TrimEnd('/');
        return $"{trimmedBase}/{signalPath}";
    }

    private static string? ResolveOtlpEndpoint(IConfiguration configuration)
    {
        var endpoint = configuration[OtlpEndpointConfigKey];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = configuration[OtlpEndpointEnvKey];
        }

        return string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
    }

    private static double ResolveTracingSamplingRatio(IConfiguration configuration)
    {
        const double defaultRatio = 1.0;

        var rawValue = configuration[TracingSamplingRatioConfigKey];

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultRatio;
        }

        if (!double.TryParse(rawValue, System.Globalization.CultureInfo.InvariantCulture, out var ratio))
        {
            return defaultRatio;
        }

        return Math.Clamp(ratio, 0.0, 1.0);
    }
}
