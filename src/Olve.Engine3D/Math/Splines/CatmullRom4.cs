using System.Diagnostics.CodeAnalysis;

namespace Olve.Engine3D.Math.Splines;

public class CatmullRom4 : CatmullRom<Vector4D<float>>
{
    private readonly Matrix4X4<float>[] _coefficients;

    [SetsRequiredMembers]
    public CatmullRom4(IReadOnlyList<KeyFrame<Vector4D<float>>> keyFrames, float? min = null, float? max = null) : base(keyFrames)
    {
        Min = Vector4D<float>.One * (min ?? float.MinValue);
        Max = Vector4D<float>.One * (max ?? float.MaxValue);

        _coefficients = new Matrix4X4<float>[KeyFrames.Length - 1];

        for (var i = 1; i < KeyFrames.Length - 2; i++)
        {
            var p0 = KeyFrames[i - 1].Value;
            var p1 = KeyFrames[i].Value;
            var p2 = KeyFrames[i + 1].Value;
            var p3 = KeyFrames[i + 2].Value;

            var keyFrameMatrix = new Matrix4X4<float>(p0, p1, p2, p3);

            _coefficients[i] = Characteristics.CatmullRom * keyFrameMatrix;
        }
    }

    protected override Vector4D<float> Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = t * _coefficients[segmentIndex];

        return Vector4D.Clamp(result, Min, Max);
    }
}