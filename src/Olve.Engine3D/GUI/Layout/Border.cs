namespace Olve.Engine3D.GUI.Layout;

public readonly record struct Border(Thickness Width, RGBA Color)
{
    public static readonly Border None = new(Thickness.Zero, new RGBA(0, 0, 0, 0));
}