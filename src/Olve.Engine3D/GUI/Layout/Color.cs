namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Color(float R, float G, float B, float A)
{
    public static Color Transparent { get; } = new(0, 0, 0, 0);
}