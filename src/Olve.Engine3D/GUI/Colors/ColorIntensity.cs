namespace Olve.Engine3D.GUI.Colors;

public readonly record struct ColorIntensity(float Value)
{
    public static readonly ColorIntensity None = new(0);
    public static readonly ColorIntensity Full = new(1);
}