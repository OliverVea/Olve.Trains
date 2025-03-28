using System.Diagnostics.CodeAnalysis;

namespace Olve.Engine3D.Math.Splines;

public class CatmullRom3 : CatmullRom<Vector3D<float>>
{
    private readonly Matrix4X3<float>[] _coefficients;

    [SetsRequiredMembers]
    public CatmullRom3(IReadOnlyList<KeyFrame<Vector3D<float>>> keyFrames, float? min = null, float? max = null) : base(keyFrames)
    {
        Min = Vector3D<float>.One * (min ?? float.MinValue);
        Max = Vector3D<float>.One * (max ?? float.MaxValue);

        _coefficients = new Matrix4X3<float>[KeyFrames.Length - 3];

        for (var i = 0; i < KeyFrames.Length - 3; i++)
        {
            var p0 = KeyFrames[i].Value;
            var p1 = KeyFrames[i + 1].Value;
            var p2 = KeyFrames[i + 2].Value;
            var p3 = KeyFrames[i + 3].Value;

            var keyFrameMatrix = new Matrix4X3<float>(p0, p1, p2, p3);

            _coefficients[i] = Characteristics.CatmullRom * keyFrameMatrix;
        }
    }

    protected override Vector3D<float> Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = t * _coefficients[segmentIndex];

        return Vector3D.Clamp(result, Min, Max);
    }
}