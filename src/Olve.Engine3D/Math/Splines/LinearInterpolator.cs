using Olve.Engine3D.Math.Splines;

namespace Olve.Engine3D.Light;

public abstract class LinearInterpolator<T>(IReadOnlyList<KeyFrame<T>> keyFrames) : IInterpolator<T>
{
    public abstract T Min { get; set; }
    public abstract T Max { get; set; }

    public T Sample(float time)
    {
        if (time <= keyFrames[0].Time)
        {
            return keyFrames[0].Value;
        }

        if (time >= keyFrames[^1].Time)
        {
            return keyFrames[^1].Value;
        }

        for (var i = 0; i < keyFrames.Count - 1; i++)
        {
            if (time >= keyFrames[i].Time && time < keyFrames[i + 1].Time)
            {
                var t = (time - keyFrames[i].Time) / (keyFrames[i + 1].Time - keyFrames[i].Time);
                var v = Lerp(keyFrames[i].Value, keyFrames[i + 1].Value, t);

                return Clamp(v, Min, Max);
            }
        }

        throw new InvalidOperationException("This should never happen.");
    }

    protected abstract T Lerp(T a, T b, float t);
    protected abstract T Clamp(T value, T min, T max);
}