namespace Olve.Engine3D.Logging;

public readonly record struct LogMessage(LogLevel Level, string Message)
{
    public string? SourcePath { get; init; }
    public int? SourceLine { get; init; }
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    public string[]? Tags { get; init; }
}