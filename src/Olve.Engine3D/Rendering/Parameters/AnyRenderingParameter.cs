using OneOf;

namespace Olve.Engine3D.Graphics;

[GenerateOneOf]
public partial class AnyRenderingParameter : OneOfBase<
    RenderingParameter.Matrix4X4,
    RenderingParameter.Vector3D,
    RenderingParameter.Float,
    RenderingParameter.Texture>
{
    public AnyRenderingParameter(RenderingParameter.Matrix4X4 value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Vector3D value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Float value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Texture value) : base(value) { }

    public string Name => Match(
        matrix4X4 => matrix4X4.Name,
        vector3D => vector3D.Name,
        f => f.Name,
        texture => texture.Name);
}