using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

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
            builder.AddConsole();
        }

        if (loggingSection.GetValue("File:Enabled", true))
        {
            var today =  DateTime.Today;
            var todayString = today.ToString("yyyy-MM-dd");
            var path = loggingSection["File:Directory"] + $"/olve.trains-{todayString}.log";
            builder.AddFile(path,
                o =>
                {
                    o.Append = true;
                });
        }

        if (loggingSection.GetValue("OpenTelemetry:Enabled", true))
        {
            var otelSection = configuration.GetSection("OpenTelemetry");
            var endpoint = otelSection["Endpoint"];
            var protocol = otelSection["Protocol"];

            var oauth2Section = otelSection.GetSection("OAuth2");
            var tokenUrl = oauth2Section["TokenUrl"];
            var clientId = oauth2Section["ClientId"];
            var clientSecret = oauth2Section["ClientSecret"];
            var scope = oauth2Section["Scope"];

            if (!string.IsNullOrEmpty(tokenUrl) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                var tempLoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
                _registrationHelper = new OtlpRegistrationHelper(endpoint, protocol, tokenUrl, clientId, clientSecret, scope, tempLoggerFactory);
            }
            else
            {
                var headers = otelSection["Headers"];
                _registrationHelper = new OtlpRegistrationHelper(endpoint, protocol, headers);
            }

            var buildConfig =
#if DEBUG
                "Debug";
#else
                "Release";
#endif

            var resource = ResourceBuilder
                .CreateDefault()
                .AddService("olve.trains")
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", buildConfig),
                    new("host.name", Environment.MachineName),
                });

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
