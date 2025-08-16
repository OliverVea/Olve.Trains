namespace Olve.Engine3D.Math.Splines;

public class NoneInterpolator<T>(T value) : IInterpolator<T>
{
    private readonly T _value = value;

    public T Min { get; set; } = value;
    public T Max { get; set; } = value;

    public T Sample(float time)
    {
        return _value;
    }

    public T Tangent(float time)
    {
        throw new NotImplementedException();
    }
}