using Olve.Engine3D.Math.Splines;

namespace Olve.Engine3D.Light;

public class Curve<T> where T : struct
{
    public IReadOnlyList<KeyFrame<T>> KeyFrames { get; }
    public InterpolationType InterpolationType { get; set; }
    public T? Min { get; set; }
    public T? Max { get; set; }

    public Curve(InterpolationType interpolationType, params IEnumerable<DayTimeKeyFrame<T>> keyFrames)
    {
        KeyFrames = keyFrames.Select(x => new KeyFrame<T>(x.Time.Value, x.Value)).ToList();
        InterpolationType = interpolationType;
    }

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