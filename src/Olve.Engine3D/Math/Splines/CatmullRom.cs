using Olve.Engine3D.Light;

namespace Olve.Engine3D.Math.Splines;

public abstract class CatmullRom<T> : IInterpolator<T>
{
    protected readonly KeyFrame<T>[] KeyFrames;
    private readonly float _min;
    private readonly float _max;

    public required T Min { get; set; }
    public required T Max { get; set; }

    protected CatmullRom(IReadOnlyList<KeyFrame<T>> keyFrames)
    {
        if (keyFrames == null || keyFrames.Count < 2)
        {
            throw new ArgumentException("At least two keyframes are required.");
        }

        if (keyFrames.Take(keyFrames.Count - 1).Zip(keyFrames.Skip(1), (a, b) => a.Time < b.Time).Any(x => !x))
        {
            throw new ArgumentException("Keyframes must be sorted by time.");
        }

        KeyFrames = PadKeyFrames(keyFrames);
        _min = KeyFrames[1].Time;
        _max = KeyFrames[^2].Time;
    }

    private static KeyFrame<T>[] PadKeyFrames(IReadOnlyList<KeyFrame<T>> keyFrames)
    {
        if (keyFrames.Count < 2)
        {
            throw new ArgumentException("At least two keyframes are required.");
        }

        var firstTime = keyFrames[0].Time - (keyFrames[1].Time - keyFrames[0].Time);
        var before = new KeyFrame<T>(firstTime, keyFrames[0].Value);

        var lastTime = 2 * keyFrames[^1].Time - keyFrames[^2].Time;
        var after = new KeyFrame<T>(lastTime, keyFrames[^1].Value);

        return keyFrames.Prepend(before).Append(after).ToArray();
    }

    public T Sample(float time)
    {
        if (time < _min || time > _max)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Time is out of range.");
        }

        var segmentIndex = GetSegmentIndex(time);

        var p1 = KeyFrames[segmentIndex];
        var p2 = KeyFrames[segmentIndex + 1];

        var tLocal = (time - p1.Time) / (p2.Time - p1.Time);

        float t2 = tLocal * tLocal, t3 = t2 * tLocal;
        var tVector = new Vector4D<float>(1, tLocal, t2, t3);

        return Sample(segmentIndex - 1, in tVector);
    }

    private int GetSegmentIndex(float time)
    {
        var segmentIndex = 1;
        for (; segmentIndex < KeyFrames.Length - 2; segmentIndex++)
        {
            if (time < KeyFrames[segmentIndex + 1].Time)
                break;
        }

        return segmentIndex;
    }

    protected abstract T Sample(int segmentIndex, in Vector4D<float> t);
}