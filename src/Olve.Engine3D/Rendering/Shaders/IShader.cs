using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.Parameters;

namespace Olve.Engine3D.Rendering.Shaders;

public interface IShader
{
    RenderingId<ShaderData> RenderingId { get; set; }
    ShaderData ShaderData { get; }

    RenderingParameters MakeParameters();
}