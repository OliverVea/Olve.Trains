using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Shaders;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes;

public static class GameSceneEntities
{
    public static readonly ShaderAssetData DefaultShaderData = new()
    {
        VertexShaderPath = new("assets/shaders/default.vert.glsl"),
        FragmentShaderPath = new("assets/shaders/default.frag.glsl"),
        ShaderParameterNames = new ShaderParameterNames
        {
            PositionAttributeName = "position",
            WorldMatrixUniformName = "world",
            ViewMatrixUniformName = "view",
            ProjectionMatrixUniformName = "projection"
        }
    };

    public static readonly Vector3D<float>[] CubeVertices =
    [
        // Top
        new (-0.5f, 0.5f, -0.5f),
        new (0.5f, 0.5f, -0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),

        // Left
        new (-0.5f, -0.5f, -0.5f),
        new (-0.5f, -0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, -0.5f),

        // Right
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (0.5f, 0.5f, -0.5f),

        // Front
        new (-0.5f, -0.5f, 0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),

        // Back
        new (-0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, 0.5f, -0.5f),
        new (-0.5f, 0.5f, -0.5f),

        // Bottom
        new (-0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (-0.5f, -0.5f, 0.5f),
    ];

    public static readonly Vector3D<float>[] CubeNormals =
    [
        // Top
        new (0, 1, 0),
        new (0, 1, 0),
        new (0, 1, 0),
        new (0, 1, 0),

        // Left
        new(-1, 0, 0),
        new(-1, 0, 0),
        new(-1, 0, 0),
        new(-1, 0, 0),

        // Right
        new(1, 0, 0),
        new(1, 0, 0),
        new(1, 0, 0),
        new(1, 0, 0),

        // Front
        new(0, 0, 1),
        new(0, 0, 1),
        new(0, 0, 1),
        new(0, 0, 1),

        // Back
        new(0, 0, -1),
        new(0, 0, -1),
        new(0, 0, -1),
        new(0, 0, -1),

        // Bottom
        new(0, -1, 0),
        new(0, -1, 0),
        new(0, -1, 0),
        new(0, -1, 0),
    ];

    public static readonly TriangleIndex[] CubeIndices =
    [
        // Top
        new(0, 1, 2),
        new (0, 2, 3),

        // Left
        new TriangleIndex(0, 1, 2) + 4u,
        new TriangleIndex(0, 2, 3) + 4u,

        // Right
        new TriangleIndex(0, 1, 2) + 8u,
        new TriangleIndex(0, 2, 3) + 8u,

        // Front
        new TriangleIndex(0, 1, 2) + 12u,
        new TriangleIndex(0, 2, 3) + 12u,

        // Back
        new TriangleIndex(0, 1, 2) + 16u,
        new TriangleIndex(0, 2, 3) + 16u,

        // Bottom
        new TriangleIndex(0, 1, 2) + 20u,
        new TriangleIndex(0, 2, 3) + 20u,
    ];
}