using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Graphics;

public class Model : RenderingTarget
{
    public required Mesh Mesh { get; set; }
    public required ShaderData ShaderData { get; set; }
}