namespace Olve.Engine3D;

public static class TimeSpanExtensions
{
    public static float InSeconds(this TimeSpan timeSpan) => (float)timeSpan.TotalSeconds;
}