using System.Runtime.Intrinsics.X86;

namespace Olve.Engine3D.Math.Splines;

public class Hermite1 : Hermite<float>
{
    private readonly Vector4D<float>[] _coefficients;

    public Hermite1(IReadOnlyList<KeyFrame<Knot>> keyFrames) : base(keyFrames)
    {
        _coefficients = new Vector4D<float>[KeyFrames.Count - 1];

        for (var i = 0; i < KeyFrames.Count - 1; i++)
        {
            var p0 = KeyFrames[i].Value.Value;
            var p1 = KeyFrames[i + 1].Value.Value;
            var m0 = KeyFrames[i].Value.TangentOut;
            var m1 = KeyFrames[i + 1].Value.TangentIn;

            var geometry = new Vector4D<float>(p0, p1, m0, m1);
            var characteristic = Characteristics.Hermite;

            _coefficients[i] = new Vector4D<float>(
                Vector4D.Dot(characteristic.Row1, geometry),
                Vector4D.Dot(characteristic.Row2, geometry),
                Vector4D.Dot(characteristic.Row3, geometry),
                Vector4D.Dot(characteristic.Row4, geometry));
        }
    }

    protected override float Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = Vector4D.Dot(t, _coefficients[segmentIndex]);
        return result;
    }
}