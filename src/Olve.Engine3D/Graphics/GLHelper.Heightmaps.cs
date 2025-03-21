using Olve.Engine3D.Graphics.OpenGL;

namespace Olve.Engine3D.Graphics;

public readonly record struct Texture2D(uint Handle, uint Width, uint Height);

public static partial class GLHelper
{
    // Heightmap has 2D vertices and sample from a 2D texture to get the height
    // It is drawn using indices

    public readonly record struct OpenGLHeightmapRegistration(VAO VAO, VBO VBO, EBO EBO, ShaderProgram ShaderProgram, Texture2D Texture);


    public static Result<OpenGLHeightmapRegistration> RegisterHeightmapInOpenGL(Heightmap heightmap)
    {
        throw new NotImplementedException();
    }

    public static Result RemoveHeightmapFromOpenGL(OpenGLHeightmapRegistration heightmapRegistration)
    {
        throw new NotImplementedException();
    }

    public static Result LoadHeightmapInOpenGL(OpenGLHeightmapRegistration heightmapRegistration, Matrix4X4<float> view, Matrix4X4<float> projection)
    {
        throw new NotImplementedException();
    }

    public static Result RenderHeightmap(OpenGLHeightmapRegistration heightmapRegistration, Matrix4X4<float> world, uint indexCount)
    {
        throw new NotImplementedException();
    }
}