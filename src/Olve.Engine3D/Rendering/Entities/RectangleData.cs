using MemoryPack;
using Olve.Engine3D.Rendering.EntityManagers;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class RectangleData
{
    public required Vector2D<float> PositionPx { get; set; }
    public required Vector2D<float> SizePx { get; set; }
    public required Vector4D<float> TintRgba { get; set; }
    public required RenderingId<TextureData> TextureId { get; set; }
    public float Depth { get; set; } = 0f;

    public Result Validate()
    {
        if (SizePx.X < 0 || SizePx.Y < 0)
            return new ResultProblem("Textured rectangle size must be positive. width={0}, height={1}.", SizePx.X, SizePx.Y);

        if (!Is01(TintRgba.X) || !Is01(TintRgba.Y) || !Is01(TintRgba.Z) || !Is01(TintRgba.W))
            return new ResultProblem("Tint color channels must be in [0,1]. RGBA=({0},{1},{2},{3})",
                TintRgba.X, TintRgba.Y, TintRgba.Z, TintRgba.W);

        if (TextureId.Id == 0)
            return new ResultProblem("TextureId must be set (non-zero)");

        return Result.Success();

        static bool Is01(float v) => v is >= 0f and <= 1f;
    }
}
