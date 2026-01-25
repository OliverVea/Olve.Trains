namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Border(Thickness Width, RGBA Color, BorderRadius Radius)
{
    public static readonly Border None = new(Thickness.Zero, new RGBA(0, 0, 0, 0), BorderRadius.None);

    // Convenience constructor for backward compatibility
    public Border(Thickness Width, RGBA Color) : this(Width, Color, BorderRadius.None) { }
}