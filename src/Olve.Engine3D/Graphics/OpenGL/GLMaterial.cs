namespace Olve.Engine3D.Graphics.OpenGL;

public class GLMaterial
{
    // Texture is not implemented yet
    // public Texture? Texture { get; set; }
    public required GLShader Shader { get; set; }
}