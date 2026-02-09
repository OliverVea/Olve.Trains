using MemoryPack;

namespace Olve.Engine3D.Assets.Entities;

[MemoryPackable]
public partial class TextureData
{
    public required Vector4D<byte>[] Pixels { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }

    public Result Validate()
    {
        Result[] results =
        [
            Pixels.Length == 0 ? new ResultProblem("Texture is empty") : Result.Success(),
            Width <= 0 ? new ResultProblem("Texture width '{0}' is less than or equal to zero", Width) : Result.Success(),
            Height <= 0 ? new ResultProblem("Texture height '{0}' is less than or equal to zero", Height) : Result.Success(),
            Pixels.Length != Width * Height
                ? new ResultProblem("Texture pixel count '{0}' does not equal width '{1}' x height '{2}' ('{3}')", Pixels.Length, Width, Height, Width * Height)
                : Result.Success()
        ];

        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}