using Olve.Engine3D.Time;

namespace Olve.Engine3D.Light;

public readonly record struct DayTimeKeyFrame<T>(DayTime Time, T Value)
{
    public static implicit operator DayTimeKeyFrame<T>((DayTime, T) tuple) => new(tuple.Item1, tuple.Item2);
}