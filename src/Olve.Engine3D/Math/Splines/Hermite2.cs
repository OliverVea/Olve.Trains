namespace Olve.Engine3D.Math.Splines;

public class Hermite2 : Hermite<Vector2D<float>>
{
    private readonly Matrix4X2<float>[] _coefficients;

    public Hermite2(IReadOnlyList<KeyFrame<Knot>> keyFrames) : base(keyFrames)
    {
        _coefficients = new Matrix4X2<float>[KeyFrames.Count - 1];

        for (var i = 0; i < KeyFrames.Count - 1; i++)
        {
            var p0 = KeyFrames[i].Value.Value;
            var p1 = KeyFrames[i + 1].Value.Value;
            var m0 = KeyFrames[i].Value.TangentOut;
            var m1 = KeyFrames[i + 1].Value.TangentIn;

            var keyFrameMatrix = new Matrix4X2<float>(p0, p1, m0, m1);

            _coefficients[i] = Characteristics.Hermite * keyFrameMatrix;
        }
    }

    protected override Vector2D<float> Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = t * _coefficients[segmentIndex];
        return result;
    }
}