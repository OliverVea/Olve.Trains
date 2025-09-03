using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.Parameters;

namespace Olve.Engine3D.Rendering.Shaders;

public enum BlendMode
{
    None,          
    Alpha,         
    Premultiplied, 
    Additive       
}

public readonly record struct RenderState(
    BlendMode Blend = BlendMode.None,
    bool DepthWrite = true
)
{
    public static readonly RenderState Opaque = new(BlendMode.None, true);
    public static readonly RenderState AlphaBlend = new(BlendMode.Alpha, false);
    public static readonly RenderState Premultiplied = new(BlendMode.Premultiplied, false);
    public static readonly RenderState Additive = new(BlendMode.Additive, false);

    public bool IsTransparent => Blend != BlendMode.None;
}

public abstract class BaseWorldShader : IShader
{
    public RenderingId<ShaderData> RenderingId { get; set; }
    public abstract ShaderData ShaderData { get; }

    public virtual RenderState BlendState { get; set; } = RenderState.Opaque;

    public abstract RenderingParameters MakeParameters();
}