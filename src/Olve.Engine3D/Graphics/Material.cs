namespace Olve.Engine3D.Graphics;

public class Material
{
    // Texture is not implemented yet
    // public Texture? Texture { get; set; }
    public required Shader Shader { get; set; }
}