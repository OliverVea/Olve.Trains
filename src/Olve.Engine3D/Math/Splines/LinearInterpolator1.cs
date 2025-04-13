namespace Olve.Engine3D.Math.Splines;

public class LinearInterpolator1(IReadOnlyList<KeyFrame<float>> keyFrames) : LinearInterpolator<float>(keyFrames)
{
    public override float Min { get; set; } = float.MinValue;
    public override float Max { get; set; } = float.MaxValue;

    protected override float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    protected override float Clamp(float value, float min, float max)
    {
        return float.Clamp(value, min, max);
    }
}