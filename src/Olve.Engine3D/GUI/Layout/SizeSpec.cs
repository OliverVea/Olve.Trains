namespace Olve.Engine3D.GUI.Layout;

public readonly record struct SizeSpec(
    Dp? PreferredWidth = null,
    Dp? PreferredHeight = null,
    float ResizingWeight = 1f,
    float? AspectRatio = null,
    FitMode FitMode = FitMode.Contain)
{
    public static SizeSpec Default { get; } = new(null, null, 1f);
    public static SizeSpec None { get; } = new(null, null, 0f);
    public static SizeSpec Zero { get; } = new(Dp.Zero, Dp.Zero);
}