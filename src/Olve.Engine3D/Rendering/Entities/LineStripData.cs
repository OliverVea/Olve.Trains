using MemoryPack;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class LineStripData
{
    public int VertexCount => Positions.Length;

    /// <summary>
    /// Positions for each vertex.
    /// </summary>
    public required Vector3D<float>[] Positions { get; set; }

    /// <summary>
    /// Colors for each vertex.
    /// </summary>
    public required Vector3D<float>[] Colors { get; set; }
    
    public Result Validate()
    {
        if (Positions.Length != VertexCount)
        {
            return new ResultProblem("Vertex position count '{0}' does not match vertex count '{1}'.", Positions.Length, VertexCount);
        }

        if (Colors.Length != VertexCount)
        {
            return new ResultProblem("Vertex color count '{0}' does not match vertex count '{1}'.", Colors.Length, VertexCount);
        }

        return Result.Success();
    }
}