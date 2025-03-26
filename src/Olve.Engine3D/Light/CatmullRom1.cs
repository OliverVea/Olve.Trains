namespace Olve.Engine3D.Light;

public class CatmullRom1 : CatmullRom<float>
{
    private readonly float _min;
    private readonly float _max;
    private readonly Vector4D<float>[] _coefficients;

    public CatmullRom1(IReadOnlyList<KeyFrame<float>> keyFrames, float min = 0f, float max = 1f) : base(keyFrames)
    {
        _min = min;
        _max = max;

        _coefficients = new Vector4D<float>[KeyFrames.Length - 1];

        for (var i = 1; i < KeyFrames.Length - 2; i++)
        {
            var p0 = KeyFrames[i - 1].Value;
            var p1 = KeyFrames[i].Value;
            var p2 = KeyFrames[i + 1].Value;
            var p3 = KeyFrames[i + 2].Value;

            var geometry = new Vector4D<float>(p0, p1, p2, p3);
            _coefficients[i] = geometry * Characteristics.CatmullRom;
        }
    }

    protected override float Sample(int segmentIndex, in Vector4D<float> t)
    {
        var result = Vector4D.Dot(t, _coefficients[segmentIndex]);
        return Math.Clamp(result, _min, _max);
    }
}