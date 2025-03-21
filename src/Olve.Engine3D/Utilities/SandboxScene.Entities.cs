using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Utilities;

public partial class SandboxScene
{
    public static readonly ShaderData DefaultShaderData = new()
    {
        VertexShaderSource = new(new("direct"), VertexShaderSource),
        FragmentShaderSource = new(new("direct"), FragmentShaderSource),
        ShaderParameterNames = new ShaderParameterNames
        {
            PositionAttributeName = "position",
            WorldMatrixUniformName = "world",
            ViewMatrixUniformName = "view",
            ProjectionMatrixUniformName = "projection"
        }
    };

    public static readonly ShaderData RayShaderData = new()
    {
        VertexShaderSource = new(new("direct"), RayVertexShaderSource),
        FragmentShaderSource = new(new("direct"), RayFragmentShaderSource),
        ShaderParameterNames = new ShaderParameterNames
        {
            PositionAttributeName = "aPos",
            WorldMatrixUniformName = null,
            ViewMatrixUniformName = "view",
            ProjectionMatrixUniformName = "projection"
        }
    };

    // Just the Top and Left faces for now
    private static readonly Model Cube = new()
    {
        ShaderData = DefaultShaderData,
        Indices =
        [
            // Top
            new(0, 1, 2),
            new (0, 2, 3),

            // Left
            new TriangleIndex(0, 1, 2) + 2u,
            new TriangleIndex(0, 2, 3) + 2u
        ],
        Vertices =
        [
            // Top
            new (0, 1, 0),
            new (1, 1, 0),
            new (1, 1, 1),
            new (0, 1, 1),

            // Left
            new (0, 0, 0),
            new (0, 0, 1),
            new (0, 1, 1),
            new (0, 1, 0),

        ],
        Normals = [
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
        ]
    };
}