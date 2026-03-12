using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Results;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Info about a rendering group, returned by <see cref="RenderingGroupManager.GetGroupsForPass"/>.
/// </summary>
public record GroupInfo(
    Id GroupId,
    Id PassId,
    IShader Shader,
    UntypedGeometryId GeometryId,
    RenderState RenderState,
    PrimitiveType PrimitiveType,
    int SortKey,
    IShaderParameters? GroupParameters);

/// <summary>
/// Prototype version of RenderingGroupManager showing the new Register signature.
/// The TFormat parameter is shared between the shader and render pass,
/// so the compiler enforces they target the same frame format.
/// </summary>
public class RenderingGroupManager
{
    private record GroupEntry(
        Id Pass,
        IShader Shader,
        UntypedGeometryId GeometryId,
        RenderState RenderState,
        PrimitiveType PrimitiveType,
        int SortKey,
        IShaderParameters? GroupParameters);

    private readonly Dictionary<Id, GroupEntry> _groups = new();
    private readonly Dictionary<Id, List<Id>> _groupsByPass = new();

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

        _groups[groupId.Value] = new GroupEntry(pass.Value, shader, geometryId, renderState, primitiveType, sortKey, groupParameters);
        GetOrCreatePassList(pass.Value).Add(groupId.Value);

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

        _groups[groupId.Value] = new GroupEntry(pass.Value, shader, geometryId, renderState, primitiveType, sortKey, groupParameters);
        GetOrCreatePassList(pass.Value).Add(groupId.Value);

        return groupId;
    }

    // ── Destroy ──

    public DeletionResult Destroy<TInstance>(GroupId<TInstance> group)
        where TInstance : IInstanceData
    {
        if (!_groups.TryGetValue(group.Value, out var entry))
            return DeletionResult.NotFound();

        _groups.Remove(group.Value);

        if (_groupsByPass.TryGetValue(entry.Pass, out var list))
        {
            list.Remove(group.Value);
            if (list.Count == 0) _groupsByPass.Remove(entry.Pass);
        }

        return DeletionResult.Success();
    }

    // ── Query ──

    /// <summary>
    /// Returns all groups registered to a pass, sorted by sort key (ascending).
    /// Uses untyped pass Id so the render loop can iterate all passes from
    /// <see cref="RenderPassManager.GetOrderedPasses"/>.
    /// </summary>
    public IReadOnlyList<GroupInfo> GetGroupsForPass(Id passId)
    {
        if (!_groupsByPass.TryGetValue(passId, out var groupIds))
            return [];

        return groupIds
            .Where(id => _groups.ContainsKey(id))
            .Select(id =>
            {
                var e = _groups[id];
                return new GroupInfo(id, e.Pass, e.Shader, e.GeometryId, e.RenderState, e.PrimitiveType, e.SortKey, e.GroupParameters);
            })
            .OrderBy(g => g.SortKey)
            .ToList();
    }

    private List<Id> GetOrCreatePassList(Id passId)
    {
        if (!_groupsByPass.TryGetValue(passId, out var list))
        {
            list = [];
            _groupsByPass[passId] = list;
        }
        return list;
    }
}
