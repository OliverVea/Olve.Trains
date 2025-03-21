using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Graphics;

public class Heightmap : RenderingTarget
{
    public required float[] Heights { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required ShaderData ShaderData { get; set; }
}