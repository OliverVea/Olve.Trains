using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Rendering.Entities;
using Olve.Engine3D.Graphics.Shaders;
using TextureData = Olve.Engine3D.Graphics.Entities.TextureData;

namespace Olve.Engine3D.Utilities;

public partial class SandboxScene
{
    public static readonly ShaderData DefaultShaderData = new()
    {
        Name = "Default",
        VertexSource = VertexShaderSource,
        FragmentSource = FragmentShaderSource,
    };

    // Just the Top and Left faces for now
    private static readonly Model Cube = new()
    {
        Material = new Material
        {
            TextureData = new TextureData()
            {
                Height = 1,
                Width = 1,
                Pixels = [ new Vector3D<byte>(255, 0, 255)]
            },
            ShaderData = DefaultShaderData,
        },
        Mesh = new()
        {
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
        }
    };
    
    


    private const string VertexShaderSource =
        """
        #version 330 core

        layout (location = 0) in vec3 position;

        uniform mat4 world;
        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
          gl_Position = projection * view * world * vec4(position, 1.0);
        }
        """;

    private const string FragmentShaderSource =
        """
        #version 330 core

        out vec4 out_color;

        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }
        """;
}