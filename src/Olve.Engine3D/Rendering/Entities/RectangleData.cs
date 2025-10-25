using MemoryPack;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class RectangleData
{
    public static readonly RectangleData Default = new()
    {
        ColorRgba = Vector4D<float>.Zero,
        PositionPx = Vector2D<float>.Zero,
        SizePx = Vector2D<float>.Zero,
        BorderColorRgba = Vector4D<float>.Zero,
        BorderPx = 0,
        CornerRadiusPx = 0,
        Depth = 0
    };

    public required Vector2D<float> PositionPx { get; set; }
    public required Vector2D<float> SizePx { get; set; }
    public required Vector4D<float> ColorRgba { get; set; }
    public float Depth { get; set; } = 0f;

    // TODO
    public float CornerRadiusPx { get; set; } = 0f;
    public float BorderPx { get; set; } = 0f;
    public Vector4D<float>? BorderColorRgba { get; set; }

    public Result Validate()
    {
        if (SizePx.X < 0 || SizePx.Y < 0)
            return new ResultProblem("Rectangle size must be positive. width={0}, height={1}.", SizePx.X, SizePx.Y);

        if (!Is01(ColorRgba.X) || !Is01(ColorRgba.Y) || !Is01(ColorRgba.Z) || !Is01(ColorRgba.W))
            return new ResultProblem("Color channels must be in [0,1]. RGBA=({0},{1},{2},{3})",
                ColorRgba.X, ColorRgba.Y, ColorRgba.Z, ColorRgba.W);

        if (BorderPx < 0 || CornerRadiusPx < 0)
            return new ResultProblem("Border and corner radius must be >= 0. border={0}, radius={1}.", BorderPx, CornerRadiusPx);

        return Result.Success();

        static bool Is01(float v) => v is >= 0f and <= 1f;
    }
}