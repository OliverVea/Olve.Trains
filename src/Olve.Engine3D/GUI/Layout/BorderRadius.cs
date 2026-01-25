namespace Olve.Engine3D.GUI.Layout;

/// <summary>
/// Represents corner radii for rounded borders. Supports uniform or per-corner radii.
/// </summary>
public readonly record struct BorderRadius(Dp TopLeft, Dp TopRight, Dp BottomRight, Dp BottomLeft)
{
    public static readonly BorderRadius None = new(Dp.Zero, Dp.Zero, Dp.Zero, Dp.Zero);

    /// <summary>Uniform radius for all corners</summary>
    public static BorderRadius All(Dp radius) => new(radius, radius, radius, radius);

    /// <summary>Check if all corners have the same radius</summary>
    public bool IsUniform => TopLeft == TopRight && TopRight == BottomRight && BottomRight == BottomLeft;

    /// <summary>Get the uniform radius if all corners are equal</summary>
    public Dp? UniformRadius => IsUniform ? TopLeft : null;
}
