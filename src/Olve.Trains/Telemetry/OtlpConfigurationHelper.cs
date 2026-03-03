using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace Olve.Trains.Telemetry;

public static class OtlpConfigurationHelper
{
    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetSection("Logging").GetValue("OpenTelemetry:Enabled", false);

    public static OtlpRegistrationHelper CreateRegistrationHelper(
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null)
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
            return new OtlpRegistrationHelper(endpoint, protocol, tokenUrl, clientId, clientSecret, scope, loggerFactory);
        }

        var headers = otelSection["Headers"];
        return new OtlpRegistrationHelper(endpoint, protocol, headers);
    }

    public static ResourceBuilder CreateResourceBuilder(IConfiguration? configuration = null)
    {
        var buildConfig =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        var otelSection = configuration?.GetSection("OpenTelemetry");

        var attributes = new List<KeyValuePair<string, object>>
        {
            new("deployment.environment", buildConfig),
            new("host.name", Environment.MachineName),
        };

        var hostClass = otelSection?["HostClass"];
        if (!string.IsNullOrEmpty(hostClass))
        {
            attributes.Add(new("host.class", hostClass));
        }

        return ResourceBuilder
            .CreateDefault()
            .AddService("olve.trains")
            .AddAttributes(attributes);
    }
}
