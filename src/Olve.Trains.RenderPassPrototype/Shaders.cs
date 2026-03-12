using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Stub default shader implementing IShader&lt;IDefaultFrameFormat&gt;.
/// In the real codebase this would be source-generated from GLSL with a @frameFormat annotation.
/// </summary>
public class DefaultShader : IShader<IDefaultFrameFormat>
{
    /// <summary>
    /// Shadow map input — same TextureId&lt;Depth&gt; whether from asset or framebuffer attachment.
    /// </summary>
    public TextureId<Depth>? ShadowMap { get; set; }

    public RenderingId<ShaderData> RenderingId { get; set; }
    public ShaderData ShaderData { get; } = new("default", "", "");
    public RenderState BlendState => RenderState.Opaque;
    public RenderingParameters MakeParameters() => throw new NotImplementedException();
}

/// <summary>
/// Stub depth-only shader implementing IShader&lt;IDepthFrameFormat&gt;.
/// No color output — writes only to the depth buffer.
/// </summary>
public class DepthOnlyShader : IShader<IDepthFrameFormat>
{
    public RenderingId<ShaderData> RenderingId { get; set; }
    public ShaderData ShaderData { get; } = new("depth_only", "", "");
    public RenderState BlendState => RenderState.Opaque;
    public RenderingParameters MakeParameters() => throw new NotImplementedException();
}

/// <summary>
/// Stub GUI shader — also targets IDefaultFrameFormat since GUI renders to the same color output.
/// </summary>
public class GuiShader : IShader<IDefaultFrameFormat>
{
    public RenderingId<ShaderData> RenderingId { get; set; }
    public ShaderData ShaderData { get; } = new("gui", "", "");
    public RenderState BlendState => RenderState.AlphaBlend;
    public RenderingParameters MakeParameters() => throw new NotImplementedException();
}
