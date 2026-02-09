namespace Olve.Engine3D.GUI.Colors;

public record ColorPicker(RGB Color, RGB Base)
{
    public RGB Sample(ColorIntensity intensity) => RGB.FromVector(
        Color.ToVector() * intensity.Value
        + Base.ToVector() * (1 - intensity.Value));
}