namespace Olve.Engine3D.Light;

public class CatmullRom2 : CatmullRom<Vector2D<float>>
{
    private readonly Vector2D<float> _min;
    private readonly Vector2D<float> _max;

    private readonly Matrix4X2<float>[] _coefficients;

    public CatmullRom2(IReadOnlyList<KeyFrame<Vector2D<float>>> keyFrames, float min = 0f, float max = 1f) : base(keyFrames)
    {
        _min = Vector2D<float>.One * min;
        _max = Vector2D<float>.One * max;

        _coefficients = new Matrix4X2<float>[KeyFrames.Length - 1];

        for (var i = 1; i < KeyFrames.Length - 2; i++)
        {
            var p0 = KeyFrames[i - 1].Value;
            var p1 = KeyFrames[i].Value;
            var p2 = KeyFrames[i + 1].Value;
            var p3 = KeyFrames[i + 2].Value;

            var keyFrameMatrix = new Matrix4X2<float>(p0, p1, p2, p3);

            _coefficients[i] = Characteristics.CatmullRom * keyFrameMatrix;
        }
    }

    protected override Vector2D<float> Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = t * _coefficients[segmentIndex];
        return Vector2D.Clamp(result, _min, _max);
    }
}