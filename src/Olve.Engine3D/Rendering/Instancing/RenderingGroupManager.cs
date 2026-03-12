using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Instancing;

public class RenderingGroupManager(
    Provider<GL> glProvider,
    GeometryManager geometryManager,
    ShaderEntityManager shaderEntityManager)
{
    private readonly Dictionary<UntypedGroupId, GroupData> _groups = new();
    private readonly Dictionary<Id, List<GroupData>> _groupsByPass = new();

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
        return RegisterCore<TInstance>(geometryId, shader, pass.Value, renderState, primitiveType, sortKey, groupParameters);
    }

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
        return RegisterCore<TInstance>(geometryId, shader, pass.Value, renderState, primitiveType, sortKey, groupParameters);
    }

    public Result Deregister(UntypedGroupId groupId)
    {
        if (!_groups.Remove(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        if (_groupsByPass.TryGetValue(groupData.PassId, out var passList))
        {
            passList.Remove(groupData);
            if (passList.Count == 0) _groupsByPass.Remove(groupData.PassId);
        }

        var gl = glProvider.Value;
        gl.DeleteVertexArray(groupData.VAO.Handle);
        gl.DeleteBuffer(groupData.InstanceVBO.Handle);

        return Result.Success();
    }

    public Result SetGroupParameters(UntypedGroupId groupId, IShaderParameters? parameters)
    {
        if (!_groups.TryGetValue(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        groupData.GroupParameters = parameters;
        return Result.Success();
    }

    internal bool TryGet(UntypedGroupId groupId, out GroupData groupData)
    {
        return _groups.TryGetValue(groupId, out groupData!);
    }

    internal IReadOnlyList<GroupData> GetGroupsForPass(Id passId)
    {
        if (!_groupsByPass.TryGetValue(passId, out var groups))
            return [];

        groups.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));
        return groups;
    }

    private Result<GroupId<TInstance>> RegisterCore<TInstance>(
        UntypedGeometryId geometryId,
        IShader shader,
        Id passId,
        RenderState renderState,
        PrimitiveType primitiveType,
        int sortKey,
        IShaderParameters? groupParameters)
        where TInstance : IInstanceData
    {
        if (!geometryManager.TryGet(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        if (shader.RenderingId == default)
        {
            if (shaderEntityManager.Register(shader.ShaderData)
                .TryPickProblems(out var shaderProblems, out var renderingId))
            {
                return shaderProblems.Prepend("Failed to register shader '{0}'", shader.ShaderData.Name);
            }

            shader.RenderingId = renderingId;
        }

        var gl = glProvider.Value;

        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        if (geoData.MeshVBO.Handle != 0 && geoData.ConfigureMeshAttributes is { } configureMesh)
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, geoData.MeshVBO.Handle);
            configureMesh(gl);
        }

        if (geoData.MeshEBO is { } ebo)
        {
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);
        }

        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, ReadOnlySpan<float>.Empty, BufferUsageARB.DynamicDraw);
        TInstance.ConfigureAttributes(gl);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        var groupId = GroupId<TInstance>.New();
        var groupData = new GroupData(
            GeometryId: geometryId,
            Shader: shader,
            RenderState: renderState,
            VAO: new VAO(vaoHandle),
            InstanceVBO: new VBO(instVboHandle, 0),
            SortKey: sortKey,
            PrimitiveType: primitiveType,
            GroupParameters: groupParameters,
            Instances: new InstanceStore<TInstance>(),
            PassId: passId);

        _groups[groupId] = groupData;

        if (!_groupsByPass.TryGetValue(passId, out var passList))
        {
            passList = [];
            _groupsByPass[passId] = passList;
        }
        passList.Add(groupData);

        return groupId;
    }
}
