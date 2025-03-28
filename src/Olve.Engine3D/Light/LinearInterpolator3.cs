using Olve.Engine3D.Math.Splines;

namespace Olve.Engine3D.Light;

public class LinearInterpolator3(IReadOnlyList<KeyFrame<Vector3D<float>>> keyFrames) : LinearInterpolator<Vector3D<float>>(keyFrames)
{
    public override Vector3D<float> Min { get; set; } = Vector3D<float>.One * float.MinValue;
    public override Vector3D<float> Max { get; set; } = Vector3D<float>.One * float.MaxValue;

    protected override Vector3D<float> Lerp(Vector3D<float> a, Vector3D<float> b, float t)
    {
        return a + (b - a) * t;
    }

    protected override Vector3D<float> Clamp(Vector3D<float> value, Vector3D<float> min, Vector3D<float> max)
    {
        return Vector3D.Clamp(value, min, max);
    }
}