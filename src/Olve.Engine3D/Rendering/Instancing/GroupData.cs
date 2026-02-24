using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Instancing;

internal sealed class GroupData(
    UntypedGeometryId GeometryId,
    IShader Shader,
    RenderState RenderState,
    VAO VAO,
    VBO InstanceVBO,
    int SortKey,
    PrimitiveType PrimitiveType,
    IShaderParameters? GroupParameters,
    IInstanceStore Instances)
{
    public UntypedGeometryId GeometryId { get; } = GeometryId;
    public IShader Shader { get; } = Shader;
    public RenderState RenderState { get; } = RenderState;
    public VAO VAO { get; } = VAO;
    public VBO InstanceVBO { get; } = InstanceVBO;
    public int SortKey { get; } = SortKey;
    public PrimitiveType PrimitiveType { get; } = PrimitiveType;
    public IShaderParameters? GroupParameters { get; set; } = GroupParameters;
    public IInstanceStore Instances { get; } = Instances;
    public bool IsDirty { get; private set; } = true;

    public void MarkDirty() => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
}
