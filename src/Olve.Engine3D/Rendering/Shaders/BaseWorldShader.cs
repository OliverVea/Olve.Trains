using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.Parameters;

namespace Olve.Engine3D.Rendering.Shaders;

public abstract class BaseWorldShader : IShader
{
    public RenderingId<ShaderData> RenderingId { get; set; }
    public abstract ShaderData ShaderData { get; }

    public virtual RenderState BlendState { get; set; } = RenderState.Opaque;

    public abstract RenderingParameters MakeParameters();
}