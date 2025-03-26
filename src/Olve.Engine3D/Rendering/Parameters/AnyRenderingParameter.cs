using System.Diagnostics;
using OneOf;

namespace Olve.Engine3D.Rendering.Parameters;

[GenerateOneOf]
[DebuggerDisplay("{DebugDisplay}")]
public partial class AnyRenderingParameter : OneOfBase<
    RenderingParameter.Matrix3X3,
    RenderingParameter.Matrix4X4,
    RenderingParameter.Vector2D,
    RenderingParameter.Vector3D,
    RenderingParameter.Float,
    RenderingParameter.Texture>
{
    public AnyRenderingParameter(RenderingParameter.Matrix4X4 value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Vector3D value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Float value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Texture value) : base(value) { }

    public string Name => Match(
        matrix3X3 => matrix3X3.Name,
        matrix4X4 => matrix4X4.Name,
        vector2D => vector2D.Name,
        vector3D => vector3D.Name,
        f => f.Name,
        texture => texture.Name);

    private string DebugDisplay => Match(
        matrix3X3 => $"Matrix3X3 {matrix3X3.Name}",
        matrix4X4 => $"Matrix4X4 {matrix4X4.Name}",
        vector2D => $"Vector2D {vector2D.Name} (X: {vector2D.Value.X}, Y: {vector2D.Value.Y})",
        vector3D => $"Vector3D {vector3D.Name} (X: {vector3D.Value.X}, Y: {vector3D.Value.Y}, Z: {vector3D.Value.Z})",
        f => $"Float {f.Name} ({f.Value})",
        texture => $"Texture {texture.Name} ({texture.Value})");
}