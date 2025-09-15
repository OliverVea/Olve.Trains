namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Border(Thickness Width, Color Color)
{
    public static readonly Border None = new(Thickness.Zero, Color.Transparent);
}