namespace Olve.Engine3D.GUI.Layout;

public readonly record struct BoxBounds(Vector2D<Dp> Position, Vector2D<Dp> Size)
{
    public Dp Width => Size.X;
    public Dp Height => Size.Y;
}