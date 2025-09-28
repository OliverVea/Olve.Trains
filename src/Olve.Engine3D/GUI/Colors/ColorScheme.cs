namespace Olve.Engine3D.GUI.Colors;

public readonly record struct ColorIntensity(float Value)
{
    public static readonly ColorIntensity None = new(0);
    public static readonly ColorIntensity Full = new(1);
}

public record ColorPicker(RGB Color, RGB Base)
{
    public RGB Sample(ColorIntensity intensity) => RGB.FromVector(
        Color.ToVector() * intensity.Value
        + Base.ToVector() * (1 - intensity.Value));
}

public class ColorScheme
{
    public required ColorPicker Background { get; init; }
    public required ColorPicker Panel { get; init; }
    public required ColorPicker Primary { get; init; }
    public required ColorPicker Secondary { get; init; }
    public required ColorPicker Tertiary { get; init; }
    public required ColorPicker Warning { get; init; }
    public required ColorPicker Info { get; init; }
    public required ColorPicker Error { get; init; }
    
    /*
    public static ColorScheme Default => new()
    {
        Background = new ColorPicker(0xAAAAAA, 0x000000)
    }
    */
}