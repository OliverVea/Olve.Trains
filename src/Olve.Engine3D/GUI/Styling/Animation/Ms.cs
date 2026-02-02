namespace Olve.Engine3D.GUI.Styling.Animation;

public readonly record struct Ms(int Value)
{
    public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(Value);
}
