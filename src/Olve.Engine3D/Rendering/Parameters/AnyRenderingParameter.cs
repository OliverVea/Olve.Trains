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
    RenderingParameter.Vector4D,
    RenderingParameter.Float,
    RenderingParameter.Bool,
    RenderingParameter.Texture,
    RenderingParameter.IntVector2D>
{
    public AnyRenderingParameter(RenderingParameter.Matrix4X4 value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Vector3D value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Vector4D value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Float value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Texture value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.IntVector2D value) : base(value) { }

    public string Name => Match(
        matrix3X3 => matrix3X3.Name,
        matrix4X4 => matrix4X4.Name,
        vector2D => vector2D.Name,
        vector3D => vector3D.Name,
        vector4D => vector4D.Name,
        f => f.Name,
        b => b.Name,
        texture => texture.Name,
        intVector2D => intVector2D.Name);

    private string DebugDisplay => Match(
        matrix3X3 => $"Matrix3X3 {matrix3X3.Name}",
        matrix4X4 => $"Matrix4X4 {matrix4X4.Name}",
        vector2D => $"Vector2D {vector2D.Name} (X: {vector2D.Value.X}, Y: {vector2D.Value.Y})",
        vector3D => $"Vector3D {vector3D.Name} (X: {vector3D.Value.X}, Y: {vector3D.Value.Y}, Z: {vector3D.Value.Z})",
        vector4D => $"Vector3D {vector4D.Name} (X: {vector4D.Value.X}, Y: {vector4D.Value.Y}, Z: {vector4D.Value.Z}, W: {vector4D.Value.W}",
        f => $"Float {f.Name} ({f.Value})",
        b => $"Bool {b.Name} ({b.Value})",
        texture => $"Texture {texture.Name} ({texture.Value})",
        intVector2D => $"IntVector2D {intVector2D.Name} (X: {intVector2D.Value.X}, Y: {intVector2D.Value.Y})");
}