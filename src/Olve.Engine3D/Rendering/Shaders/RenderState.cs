namespace Olve.Engine3D.Rendering.Shaders;

public readonly record struct RenderState(
    BlendMode Blend = BlendMode.None,
    bool DepthWrite = true,
    bool DepthTest = true
)
{
    public static readonly RenderState Opaque = new(BlendMode.None, true);
    public static readonly RenderState AlphaBlend = new(BlendMode.Alpha, true);
    public static readonly RenderState AlphaBlendNoDepthWrite = new(BlendMode.Alpha, false);
    public static readonly RenderState PremultipliedNoDepthWrite = new(BlendMode.Premultiplied, false);
    public static readonly RenderState AdditiveNoDepthWrite = new(BlendMode.Additive, false);
    public static readonly RenderState AlphaBlendNoDepth = new(BlendMode.Alpha, false, false);

    public bool IsTransparent => Blend != BlendMode.None;
}