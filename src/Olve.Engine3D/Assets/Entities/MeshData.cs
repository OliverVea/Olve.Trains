using MemoryPack;
using TriangleIndex = Olve.Engine3D.Rendering.Primitives.TriangleIndex;

namespace Olve.Engine3D.Assets.Entities;

[MemoryPackable]
public partial class MeshData
{
    public int VertexCount => Positions.Length;

    /// <summary>
    /// Vertices for each triangle.
    /// </summary>
    public required Vector3D<float>[] Positions { get; set; }

    /// <summary>
    /// Normals for each vertex.
    /// </summary>
    public required Vector3D<float>[] Normals { get; set; }

    /// <summary>
    /// Indices for each triangle.
    /// </summary>
    public required TriangleIndex[] Indices { get; set; }

    /// <summary>
    /// Texture coordinates for each vertex.
    /// </summary>
    public required Vector2D<float>[] TextureCoordinates { get; set; }

    public Result Validate()
    {
        Result[] results =
        [
            Positions.Length != VertexCount ? new ResultProblem("Vertex position count '{0}' does not match vertex count '{1}'.", Positions.Length, VertexCount) : Result.Success(),
            Normals.Length != VertexCount ? new ResultProblem("Vertex normal count '{0}' does not match vertex count '{1}'.", Normals.Length, VertexCount) : Result.Success(),
            TextureCoordinates.Length != VertexCount ? new ResultProblem("Vertex texture coordinate count '{0}' does not match vertex count '{1}'.", TextureCoordinates.Length, VertexCount) : Result.Success(),
            Indices.SelectMany(x => new [] {x.A, x.B, x.C}).Any(x => x >= Positions.Length) ? new ResultProblem("Triangle indices exceed vertex indices") : Result.Success()
        ];

        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}