using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenTelemetry.Logs;

namespace Olve.Trains.Telemetry;

public static class LoggingExtensions
{
    private static OtlpRegistrationHelper? _registrationHelper;

    public static ILoggingBuilder AddConfiguredLogging(
        this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        builder.ClearProviders();

        var loggingSection = configuration.GetSection("Logging");
        builder.AddConfiguration(loggingSection);

        if (loggingSection.GetValue("Console:Enabled", true))
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            });
        }

        if (loggingSection.GetValue("File:Enabled", true))
        {
            var today = DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");
            var path = loggingSection["File:Directory"] + $"/olve.trains-{todayString}.log";
            builder.AddFile(path, o => { o.Append = true; });
        }

        if (OtlpConfigurationHelper.IsEnabled(configuration))
        {
            var tempLoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            _registrationHelper = OtlpConfigurationHelper.CreateRegistrationHelper(configuration, tempLoggerFactory);
            var resource = OtlpConfigurationHelper.CreateResourceBuilder();

            builder.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resource);
                logging.IncludeScopes = true;
                logging.IncludeFormattedMessage = true;
                logging.AddProcessor(new SeverityNumberProcessor());
                logging.AddOtlpExporter(options => _registrationHelper.RegisterLogs(options));
            });
        }

        return builder;
    }

    public static void ShutdownLogging()
    {
        _registrationHelper?.Dispose();
    }
}
