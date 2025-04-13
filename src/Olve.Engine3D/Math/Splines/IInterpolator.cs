namespace Olve.Engine3D.Math.Splines;

public interface IInterpolator<T>
{
    public T Min { get; set; }
    public T Max { get; set; }

    T Sample(float time);
}