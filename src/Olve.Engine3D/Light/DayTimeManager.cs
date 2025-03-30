namespace Olve.Engine3D.Light;

public class DayTimeManager
{
    public static readonly TimeSpan DefaultDayLength = TimeSpan.FromMinutes(20);
    public TimeSpan DayLength { get; set; } = DefaultDayLength;
    public DayTime CurrentTime { get; set; } = new(0);

    public Action? OnDayStart { get; set; }


    public Result Step(TimeSpan timeSpan)
    {
        if (DayLength == TimeSpan.Zero)
        {
            return new ResultProblem("Day length is zero.");
        }

        var deltaTime = (float)(timeSpan.TotalHours / DayLength.TotalHours);

        CurrentTime = new DayTime(CurrentTime.Value + deltaTime * 24);

        while (CurrentTime.Value >= 24)
        {
            CurrentTime = new DayTime(CurrentTime.Value - 24);
            OnDayStart?.Invoke();
        }

        return Result.Success();
    }
}