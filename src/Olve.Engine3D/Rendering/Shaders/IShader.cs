using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.Parameters;

namespace Olve.Engine3D.Rendering.Shaders;

public interface IShader
{
    RenderingId<ShaderData> RenderingId { get; set; }
    ShaderData ShaderData { get; }
    RenderState BlendState { get; }

    RenderingParameters MakeParameters();
}