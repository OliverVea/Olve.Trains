using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenTelemetry.Logs;

namespace Olve.Trains.Shared.Telemetry;

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

        if (loggingSection.GetValue("File:Enabled", false))
        {
            var today = DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");
            var directory = loggingSection["File:Directory"] ?? "logs";
            var expandedDirectory = directory.StartsWith("~/")
                ? (Olve.Paths.Path.GetHomeDirectory() / directory[2..]).Path
                : directory;
            var path = expandedDirectory + $"/olve.trains-{todayString}.log";
            builder.AddFile(path, o => { o.Append = true; });
        }

        if (OtlpConfigurationHelper.IsEnabled(configuration))
        {
            var tempLoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            _registrationHelper = OtlpConfigurationHelper.CreateRegistrationHelper(configuration, tempLoggerFactory);
            var resource = OtlpConfigurationHelper.CreateResourceBuilder(configuration);

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
