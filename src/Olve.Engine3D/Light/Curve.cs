using Olve.Engine3D.Math.Splines;

namespace Olve.Engine3D.Light;

public class Curve<T>(InterpolationType interpolationType, params IEnumerable<DayTimeKeyFrame<T>> keyFrames) where T : struct
{
    public IReadOnlyList<KeyFrame<T>> KeyFrames { get; } = keyFrames.Select(x => new KeyFrame<T>(x.Time.Value, x.Value)).ToList();
    public InterpolationType InterpolationType { get; } = interpolationType;
    public T? Min { get; init; }
    public T? Max { get; init; }

    public Result Validate()
    {
        if (KeyFrames.Count < 2)
        {
            return  new ResultProblem("At least two keyframes are required, but only '{0}' was provided.", KeyFrames.Count);
        }

        if (!KeyFrames.Select(x => x.Time).IsInOrder())
        {
            return new ResultProblem("Keyframes must be sorted by time.");
        }

        return Result.Success();
    }
}