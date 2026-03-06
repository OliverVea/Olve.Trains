using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Olve.Trains.Shared.Telemetry;

public class OtlpRegistrationHelper : IDisposable
{
    private readonly string? _baseEndpoint;
    private readonly string? _protocol;
    private readonly string? _staticHeaders;
    private readonly OAuth2TokenProvider? _tokenProvider;

    public OtlpRegistrationHelper(string? endpoint, string? protocol, string? headers)
    {
        _baseEndpoint = endpoint?.TrimEnd('/');
        _protocol = protocol;
        _staticHeaders = headers;
        _tokenProvider = null;
    }

    public OtlpRegistrationHelper(
        string? endpoint,
        string? protocol,
        string tokenUrl,
        string clientId,
        string clientSecret,
        string? scope = null,
        ILoggerFactory? loggerFactory = null)
    {
        _baseEndpoint = endpoint?.TrimEnd('/');
        _protocol = protocol;
        _staticHeaders = null;
        _tokenProvider = new OAuth2TokenProvider(
            tokenUrl, clientId, clientSecret, scope,
            loggerFactory?.CreateLogger<OAuth2TokenProvider>());
    }

    public void RegisterLogs(OtlpExporterOptions options) => Register(options, "/v1/logs");
    public void RegisterMetrics(OtlpExporterOptions options) => Register(options, "/v1/metrics");

    private void Register(OtlpExporterOptions options, string signalPath)
    {
        if (!string.IsNullOrWhiteSpace(_baseEndpoint))
        {
            options.Endpoint = new Uri($"{_baseEndpoint}{signalPath}");
        }

        if (string.Equals(_protocol, "http", StringComparison.OrdinalIgnoreCase))
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        }

        if (_tokenProvider is not null)
        {
            var token = _tokenProvider.GetAccessToken();
            options.Headers = $"Authorization=Bearer {token}";
        }
        else if (!string.IsNullOrWhiteSpace(_staticHeaders))
        {
            options.Headers = _staticHeaders;
        }
    }

    public void Dispose()
    {
        _tokenProvider?.Dispose();
    }
}
