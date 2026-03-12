using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Results;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Prototype version of RenderingGroupManager showing the new Register signature.
/// The TFormat parameter is shared between the shader and render pass,
/// so the compiler enforces they target the same frame format.
/// </summary>
public class RenderingGroupManager
{
    private record GroupEntry(
        Id Pass,
        RenderState RenderState,
        PrimitiveType PrimitiveType,
        int SortKey,
        IShaderParameters? GroupParameters);

    private readonly Dictionary<Id, GroupEntry> _groups = new();

    /// <summary>
    /// Register a rendering group into a typed render pass.
    /// The compiler enforces that the shader's TFormat matches the pass's TFormat.
    /// </summary>
    public Result<GroupId<TInstance>> Register<TVertex, TInstance, TFormat>(
        GeometryId<TVertex> geometryId,
        IShader<TFormat> shader,
        Id<RenderPass<TFormat>> pass,
        RenderState renderState,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        int sortKey = 0,
        IShaderParameters? groupParameters = null)
        where TVertex : IVertexData
        where TInstance : IInstanceData<TVertex>
        where TFormat : IFrameFormat
    {
        var groupId = GroupId<TInstance>.New();

        _groups[groupId.Value] = new GroupEntry(pass.Value, renderState, primitiveType, sortKey, groupParameters);

        return groupId;
    }

    /// <summary>
    /// Register a rendering group using DrawArrays (no index buffer) into a typed render pass.
    /// </summary>
    public Result<GroupId<TInstance>> RegisterDrawArrays<TInstance, TFormat>(
        UntypedGeometryId geometryId,
        IShader<TFormat> shader,
        Id<RenderPass<TFormat>> pass,
        RenderState renderState,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        int sortKey = 0,
        IShaderParameters? groupParameters = null)
        where TInstance : IInstanceData
        where TFormat : IFrameFormat
    {
        var groupId = GroupId<TInstance>.New();

        _groups[groupId.Value] = new GroupEntry(pass.Value, renderState, primitiveType, sortKey, groupParameters);

        return groupId;
    }
}
