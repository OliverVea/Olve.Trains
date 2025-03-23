using MemoryPack;

namespace Olve.Engine3D.Graphics.Entities;

[MemoryPackable]
public partial class MeshData
{
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
            Normals.Length != Positions.Length ? new ResultProblem("Vertex normal count '{0}' does not match position count '{1}'.", Normals.Length, Positions.Length) : Result.Success(),
            TextureCoordinates.Length != Positions.Length ? new ResultProblem("Vertex texture coordinate count '{0}' does not match position count '{1}'.", Normals.Length, Positions.Length) : Result.Success(),
            Indices.SelectMany(x => new [] {x.A, x.B, x.C}).Any(x => x >= Positions.Length) ? new ResultProblem("Triangle indices exceed vertex indices") : Result.Success()
        ];

        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}