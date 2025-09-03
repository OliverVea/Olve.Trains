namespace Olve.Engine3D.Time;

/// <summary>
/// Represents a time of day.
/// </summary>
/// <param name="Value">The time of day as hours between 0 and 24.</param>
public readonly record struct DayTime(float Value) : IComparable<DayTime>
{
    public DayTime(int hours, int minutes = 0, int seconds = 0) : this(hours + minutes / 60f + seconds / 3600f)
    {
    }

    public int Hours => (int)Value;
    public int Minutes => (int)((Value - Hours) * 60);

    public int CompareTo(DayTime other)
    {
        return Value.CompareTo(other.Value);
    }
    
    public static bool operator <(DayTime left, DayTime right) => left.Value < right.Value;
    public static bool operator >(DayTime left, DayTime right) => left.Value > right.Value;
    public static bool operator <=(DayTime left, DayTime right) => left.Value <= right.Value;
    public static bool operator >=(DayTime left, DayTime right) => left.Value >= right.Value;
    
    public static DayTimeSpan operator -(DayTime left, DayTime right)
        => new(left.Value - right.Value);
}