namespace Olve.Engine3D.Light;

public class CatmullRom4 : CatmullRom<Vector4D<float>>
{
    private readonly Vector4D<float> _min;
    private readonly Vector4D<float> _max;

    private readonly Matrix4X4<float>[] _coefficients;

    public CatmullRom4(IReadOnlyList<KeyFrame<Vector4D<float>>> keyFrames, float min = 0f, float max = 1f) : base(keyFrames)
    {
        _min = Vector4D<float>.One * min;
        _max = Vector4D<float>.One * max;

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
        return Vector4D.Clamp(result, _min, _max);
    }
}