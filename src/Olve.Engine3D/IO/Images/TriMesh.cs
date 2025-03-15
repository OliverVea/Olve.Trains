using Microsoft.Xna.Framework.Graphics;

namespace Olve.Engine3D.IO.Images;

public class TriMesh
{
    public required VertexPosition[] Vertices { get; set; }
    public required int[] Indices { get; set; }
}