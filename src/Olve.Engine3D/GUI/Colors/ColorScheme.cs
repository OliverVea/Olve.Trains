namespace Olve.Engine3D.GUI.Colors;

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