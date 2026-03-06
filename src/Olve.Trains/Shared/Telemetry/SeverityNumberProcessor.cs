using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Olve.Trains.Shared.Telemetry;

public class SeverityNumberProcessor : BaseProcessor<LogRecord>
{
    private static int ToSeverityNumber(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => 1,
            LogLevel.Debug => 5,
            LogLevel.Information => 9,
            LogLevel.Warning => 13,
            LogLevel.Error => 17,
            LogLevel.Critical => 21,
            _ => 0,
        };

    public override void OnEnd(LogRecord data)
    {
        var attributes = data.Attributes?.ToList() ?? [];
        attributes.Add(new("severity_number", ToSeverityNumber(data.LogLevel)));
        data.Attributes = attributes;
    }
}