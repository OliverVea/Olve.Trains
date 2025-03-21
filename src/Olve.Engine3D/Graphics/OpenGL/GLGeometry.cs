using Silk.NET.OpenGL;

namespace Olve.Engine3D.Graphics.OpenGL;

public class GLGeometry
{
    public VAO VAO { get; set; }
    public VBO VBO { get; set; }
    public EBO EBO { get; set; }
    public GLEnum PrimitiveType { get; set; }
}