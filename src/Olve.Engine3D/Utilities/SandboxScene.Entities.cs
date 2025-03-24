using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Utilities;

public partial class SandboxScene
{
    public static readonly ShaderData DefaultShaderData = new()
    {
        Name = "Default",
        VertexSource = VertexShaderSource,
        FragmentSource = FragmentShaderSource,
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