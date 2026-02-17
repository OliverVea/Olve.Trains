using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var resource = OtlpConfigurationHelper.CreateResourceBuilder();

        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddMeter("Olve.Engine3D")
            .AddOtlpExporter(options => registrationHelper.RegisterMetrics(options))
            .Build();

        return services;
    }

    public static void ShutdownMetrics()
    {
        _meterProvider?.ForceFlush();
        _meterProvider?.Dispose();
        _meterProvider = null;
    }
}
