namespace Olve.Engine3D.Graphics;

public class Geometry
{
    public VAO VAO { get; set; }
    public VBO VBO { get; set; }
    public EBO EBO { get; set; }
    public uint VerticesCount { get; set; }
    public uint IndicesCount { get; set; }
}