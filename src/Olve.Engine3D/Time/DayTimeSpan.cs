namespace Olve.Engine3D.Time;

/// <summary>
/// Represents a span of time in hours (can be fractional).
/// </summary>
/// <param name="Value">The span in hours, can be negative.</param>
public readonly record struct DayTimeSpan(float Value)
{
    public DayTimeSpan(int hours = 0, int minutes = 0, int seconds = 0) 
        : this(hours + minutes / 60f + seconds / 3600f)
    {
    }
    
    public float TotalHours => Value;
    public float TotalMinutes => Value * 60f;
    public float TotalSeconds => Value * 3600f;

    public int Hours => (int)Value;
    public int Minutes => (int)((Value - Hours) * 60);
    
    public static DayTime operator +(DayTime time, DayTimeSpan span) => new(time.Value + span.Value);
    public static DayTime operator -(DayTime time, DayTimeSpan span) => new(time.Value - span.Value);
    public static DayTimeSpan operator +(DayTimeSpan left, DayTimeSpan right) => new(left.Value + right.Value);
    public static DayTimeSpan operator -(DayTimeSpan left, DayTimeSpan right) => new(left.Value - right.Value);
    public static DayTimeSpan operator *(DayTimeSpan span, float factor) => new(span.Value * factor);
    public static DayTimeSpan operator /(DayTimeSpan span, float divisor) => new(span.Value / divisor);
}