using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Olve.Trains.Telemetry;

public static class MetricsExtensions
{
    private static MeterProvider? _meterProvider;

    public static IServiceCollection AddConfiguredMetrics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!OtlpConfigurationHelper.IsEnabled(configuration))
        {
            return services;
        }

        var registrationHelper = OtlpConfigurationHelper.CreateRegistrationHelper(configuration);
        var resource = OtlpConfigurationHelper.CreateResourceBuilder(configuration);

        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddMeter("Olve.Engine3D")
            .AddOtlpExporter(options => registrationHelper.RegisterMetrics(options))
            .Build();

        EngineMetrics.IsEnabled = true;

        return services;
    }

    public static void ShutdownMetrics()
    {
        _meterProvider?.ForceFlush();
        _meterProvider?.Dispose();
        _meterProvider = null;
        EngineMetrics.IsEnabled = false;
    }
}
