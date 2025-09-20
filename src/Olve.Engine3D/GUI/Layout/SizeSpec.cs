namespace Olve.Engine3D.GUI.Layout;

public readonly record struct SizeSpec(
    Dp? PreferredWidth = null,
    Dp? PreferredHeight = null,
    float ResizingWeight = 1f)
{
    public static SizeSpec None { get; } = new();
    public static SizeSpec Zero { get; } = new(Dp.Zero, Dp.Zero);
}