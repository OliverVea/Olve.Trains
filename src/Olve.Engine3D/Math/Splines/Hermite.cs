namespace Olve.Engine3D.Math.Splines;

public abstract class Hermite<T> : IInterpolator<T>
{
    public readonly record struct Knot(T Value, T TangentIn, T TangentOut);

    private static Exception MinAndMaxNotSupportedException => new NotSupportedException($"Min and Max operations are not supported by {nameof(Hermite<T>)}");
    public T Min { get => throw MinAndMaxNotSupportedException; set => throw MinAndMaxNotSupportedException; }
    public T Max { get => throw MinAndMaxNotSupportedException; set => throw MinAndMaxNotSupportedException; }

    protected readonly IReadOnlyList<KeyFrame<Knot>> KeyFrames;

    protected Hermite(IReadOnlyList<KeyFrame<Knot>> keyFrames)
    {
        if (keyFrames == null || keyFrames.Count < 2)
        {
            throw new ArgumentException("At least two keyframes are required.");
        }

        if (keyFrames.Take(keyFrames.Count - 1).Zip(keyFrames.Skip(1), (a, b) => a.Time < b.Time).Any(x => !x))
        {
            throw new ArgumentException("Keyframes must be sorted by time.");
        }

        KeyFrames = keyFrames;
    }

    public T Sample(float time)
    {
        if (time < KeyFrames[0].Time || time > KeyFrames[^1].Time)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Time is out of range.");
        }

        var segmentIndex = GetSegmentIndex(time);

        var tLocal = (time - KeyFrames[segmentIndex].Time) / (KeyFrames[segmentIndex + 1].Time - KeyFrames[segmentIndex].Time);
        float t2 = tLocal * tLocal, t3 = t2 * tLocal;

        var tVector = new Vector4D<float>(t3, t2, tLocal, 1);

        return Sample(segmentIndex, in tVector);
    }

    private int GetSegmentIndex(float time)
    {
        for (var i = 0; i < KeyFrames.Count - 1; i++)
        {
            if (time >= KeyFrames[i].Time && time <= KeyFrames[i + 1].Time)
            {
                return i;
            }
        }

        throw new InvalidOperationException("This should never happen.");
    }

    protected abstract T Sample(int segmentIndex, in Vector4D<float> t);
}