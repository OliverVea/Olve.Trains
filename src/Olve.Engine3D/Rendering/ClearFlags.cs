namespace Olve.Engine3D.Rendering;

[Flags]
public enum ClearFlags
{
    None = 0,
    Color = 1,
    Depth = 2,
    ColorDepth = Color | Depth,
}
