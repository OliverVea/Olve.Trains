namespace Olve.Engine3D.Light;

/// <summary>
/// Represents a time of day.
/// </summary>
/// <param name="Value">The time of day as hours between 0 and 24.</param>
public readonly record struct DayTime(float Value)
{
    public DayTime(int hours, int minutes = 0, int seconds = 0) : this(hours + minutes / 60f + seconds / 3600f)
    {
    }
}