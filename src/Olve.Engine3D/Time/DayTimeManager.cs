namespace Olve.Engine3D.Time;

public class DayTimeManager
{
    public static readonly TimeSpan DefaultDayLength = TimeSpan.FromMinutes(20);
    
    public TimeSpan DayLength { get; set; } = DefaultDayLength;

    public DayTime CurrentTime
    {
        get => new((float)(_totalGameHours % 24.0));
        set => _totalGameHours = double.Round(_totalGameHours / 24.0) * 24.0 + value.Value % 24;
    }

    /// <summary>
    /// Total elapsed in-game time, in game-hours. Unlike <see cref="CurrentTime"/> (time of day only),
    /// this is the full clock and round-trips both the current day and time of day for save/load.
    /// </summary>
    public double TotalGameHours
    {
        get => _totalGameHours;
        set => _totalGameHours = value;
    }

    public long AbsoluteDays => AbsoluteHours / 24;
    public long AbsoluteHours => AbsoluteMinutes / 60;
    public long AbsoluteMinutes => (long)double.Floor(_totalGameHours * 60.0);

    public Action? OnDayStart { get; set; }

    private double _totalGameHours;

    public Result Step(TimeSpan timeSpan)
    {
        if (DayLength == TimeSpan.Zero)
        {
            return new ResultProblem("Day length is zero.");
        }

        var deltaGameHours = timeSpan.TotalHours / DayLength.TotalHours * 24.0;

        var newValue = CurrentTime.Value + (float)deltaGameHours;
        while (newValue >= 24f)
        {
            newValue -= 24f;
            OnDayStart?.Invoke();
        }
        CurrentTime = new DayTime(newValue);

        return Result.Success();
    }

    /// <summary>Convenience for scheduling: absolute minutes at span from now.</summary>
    public long InMinutesFromNow(DayTimeSpan span) => AbsoluteMinutes + (long)double.Round(span.TotalMinutes);
}