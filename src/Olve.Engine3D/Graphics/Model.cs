using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Graphics;

public class Model : RenderingTarget
{
    public required Vector3D<float>[] Vertices { get; set; }
    public required Vector3D<float>[] Normals { get; set; }
    public required TriangleIndex[] Indices { get; set; }
    public required ShaderData ShaderData { get; set; }
}