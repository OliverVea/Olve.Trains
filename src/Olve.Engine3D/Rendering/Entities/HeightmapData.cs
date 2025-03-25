using MemoryPack;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class HeightmapData
{
    public required float[] Heights { get; set; }
    public required int Width { get; set; }
    public required int Length { get; set; }

    public Result Validate(int minHeight = -128, int maxHeight = 127, int maxLength = 4096, int maxWidth = 4096)
    {
        if (Width < 1)
        {
            return new ResultProblem("Width must be greater than 0 but was '{0}'", Width);
        }

        if (Width > maxWidth)
        {
            return new ResultProblem("Width must be less than or equal to '{0}' but was '{1}'", maxWidth, Width);
        }

        if (Length < 1)
        {
            return new ResultProblem("Length must be greater than 0 but was '{0}'", Length);
        }

        if (Length > maxLength)
        {
            return new ResultProblem("Length must be less than or equal to '{0}' but was '{1}'", maxLength, Length);
        }

        if (Heights.Length != Width * Length)
        {
            return new ResultProblem("Heights array length must be equal to Width * Length ('{0}') but was '{1}'",
                Width * Length, Heights.Length);
        }

        foreach (var height in Heights)
        {
            if (height > maxHeight)
            {
                return new ResultProblem("Heightmap height '{0}' is greater than the maximum height '{1}'", height,
                    maxHeight);
            }

            if (height < minHeight)
            {
                return new ResultProblem("Heightmap height '{0}' is less than the minimum height '{1}'", height,
                    minHeight);
            }
        }

        return Result.Success();
    }
}