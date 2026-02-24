using System.Runtime.CompilerServices;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager(
    Provider<GL> glProvider,
    OpenGLShaderManager openGLShaderManager,
    ShaderEntityManager shaderEntityManager)
{
    private readonly Dictionary<UntypedGeometryId, GeometryData> _geometries = new();
    private readonly Dictionary<UntypedGroupId, GroupData> _groups = new();
    private readonly List<GroupData> _sortedGroups = [];
    private bool _sortDirty;

    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    #region Geometry

    public Result<GeometryId<TVertex>> RegisterGeometry<TVertex>(
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<uint> indices,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        BufferUsageARB usage = BufferUsageARB.StaticDraw)
        where TVertex : IVertexData
    {
        var gl = glProvider.Value;

        // Marshal vertices to float array
        var floats = MarshalVertices(vertices);

        // Create mesh VBO
        var meshVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, meshVboHandle);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, floats, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // Create EBO if indices provided
        EBO? ebo = null;
        if (indices.Length > 0)
        {
            var eboHandle = gl.CreateBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboHandle);
            gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            ebo = new EBO(eboHandle, (uint)indices.Length);
        }

        var id = GeometryId<TVertex>.New();
        _geometries[id] = new GeometryData(
            MeshVBO: new VBO(meshVboHandle, (uint)vertices.Length),
            MeshEBO: ebo,
            VertexCount: (uint)vertices.Length,
            PrimitiveType: primitiveType,
            ConfigureMeshAttributes: TVertex.ConfigureAttributes);

        return id;
    }

    public Result<UntypedGeometryId> RegisterDrawArraysGeometry(
        uint vertexCount,
        PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        var id = UntypedGeometryId.New();
        _geometries[id] = new GeometryData(
            MeshVBO: new VBO(0, vertexCount),
            MeshEBO: null,
            VertexCount: vertexCount,
            PrimitiveType: primitiveType,
            ConfigureMeshAttributes: null);

        return id;
    }

    public Result UpdateGeometryVertices<TVertex>(
        GeometryId<TVertex> geometryId,
        ReadOnlySpan<TVertex> vertices,
        BufferUsageARB usage = BufferUsageARB.DynamicDraw)
        where TVertex : IVertexData
    {
        if (!_geometries.TryGetValue(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        var gl = glProvider.Value;
        var floats = MarshalVertices(vertices);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, geoData.MeshVBO.Handle);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, floats, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // Update stored vertex count
        _geometries[geometryId] = geoData with
        {
            MeshVBO = new VBO(geoData.MeshVBO.Handle, (uint)vertices.Length),
            VertexCount = (uint)vertices.Length,
        };

        return Result.Success();
    }

    public Result DeregisterGeometry(UntypedGeometryId geometryId)
    {
        if (!_geometries.Remove(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        var gl = glProvider.Value;
        if (geoData.MeshVBO.Handle != 0)
            gl.DeleteBuffer(geoData.MeshVBO.Handle);
        if (geoData.MeshEBO is { } ebo)
            gl.DeleteBuffer(ebo.Handle);

        return Result.Success();
    }

    #endregion

    #region Groups

    public Result<GroupId<TInstance>> RegisterGroup<TVertex, TInstance>(
        GeometryId<TVertex> geometryId,
        IShader shader,
        RenderState renderState,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        int sortKey = 0,
        IShaderParameters? groupParameters = null)
        where TVertex : IVertexData
        where TInstance : IInstanceData<TVertex>
    {
        return RegisterGroupCore<TInstance>(geometryId, shader, renderState, primitiveType, sortKey, groupParameters);
    }

    public Result<GroupId<TInstance>> RegisterDrawArraysGroup<TInstance>(
        UntypedGeometryId geometryId,
        IShader shader,
        RenderState renderState,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        int sortKey = 0,
        IShaderParameters? groupParameters = null)
        where TInstance : IInstanceData
    {
        return RegisterGroupCore<TInstance>(geometryId, shader, renderState, primitiveType, sortKey, groupParameters);
    }

    private Result<GroupId<TInstance>> RegisterGroupCore<TInstance>(
        UntypedGeometryId geometryId,
        IShader shader,
        RenderState renderState,
        PrimitiveType primitiveType,
        int sortKey,
        IShaderParameters? groupParameters)
        where TInstance : IInstanceData
    {
        if (!_geometries.TryGetValue(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        // Ensure shader is registered
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

        // Create VAO for this group
        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        // Bind mesh VBO and configure vertex attributes (if geometry has vertices)
        if (geoData.MeshVBO.Handle != 0 && geoData.ConfigureMeshAttributes is { } configureMesh)
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, geoData.MeshVBO.Handle);
            configureMesh(gl);
        }

        // Bind EBO if present
        if (geoData.MeshEBO is { } ebo)
        {
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);
        }

        // Create instance VBO (initially empty)
        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, ReadOnlySpan<float>.Empty, BufferUsageARB.DynamicDraw);
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
            Instances: new InstanceStore<TInstance>());

        _groups[groupId] = groupData;
        _sortedGroups.Add(groupData);
        _sortDirty = true;

        return groupId;
    }

    public Result DeregisterGroup(UntypedGroupId groupId)
    {
        if (!_groups.Remove(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        _sortedGroups.Remove(groupData);

        var gl = glProvider.Value;
        gl.DeleteVertexArray(groupData.VAO.Handle);
        gl.DeleteBuffer(groupData.InstanceVBO.Handle);

        return Result.Success();
    }

    #endregion

    #region Instances

    public Result<Id<TInstance>> AddInstance<TInstance>(GroupId<TInstance> groupId, TInstance data)
        where TInstance : IInstanceData
    {
        if (!_groups.TryGetValue(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        var instanceId = Id.New<TInstance>();
        store.Add(instanceId, data);
        groupData.MarkDirty();

        return instanceId;
    }

    public Result UpdateInstance<TInstance>(GroupId<TInstance> groupId, Id<TInstance> instanceId, TInstance data)
        where TInstance : IInstanceData
    {
        if (!_groups.TryGetValue(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        if (!store.Update(instanceId, data))
        {
            return new ResultProblem("Instance with id '{0}' not found in group '{1}'", instanceId, groupId);
        }

        groupData.MarkDirty();
        return Result.Success();
    }

    public Result RemoveInstance<TInstance>(GroupId<TInstance> groupId, Id<TInstance> instanceId)
        where TInstance : IInstanceData
    {
        if (!_groups.TryGetValue(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        if (!store.Remove(instanceId))
        {
            return new ResultProblem("Instance with id '{0}' not found in group '{1}'", instanceId, groupId);
        }

        groupData.MarkDirty();
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

    #endregion

    #region Render

    public Result RenderAll()
    {
        if (_sortDirty)
        {
            _sortedGroups.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));
            _sortDirty = false;
        }

        var gl = glProvider.Value;

        foreach (var group in _sortedGroups)
        {
            // Rebuild instance buffer if dirty
            if (group.IsDirty)
            {
                RebuildInstanceBuffer(gl, group);
                group.ClearDirty();
            }

            var instanceCount = group.Instances.Count;
            if (instanceCount == 0) continue;

            var shader = group.Shader;
            if (shader.RenderingId == default) continue;

            if (shaderEntityManager.GetRegistration(shader.RenderingId)
                .TryPickProblems(out var problems, out var shaderRegistration))
            {
                return problems.Prepend("Failed to get shader registration for '{0}'", shader.ShaderData.Name);
            }

            // Set blend/depth state
            ApplyRenderState(gl, group.RenderState);

            // Load shader and set uniforms
            var shaderParameters = shader.MakeParameters();
            if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, shaderParameters)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to load shader '{0}'", shader.ShaderData.Name);
            }

            // Apply per-group uniform overrides
            if (group.GroupParameters is { } groupParams)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, groupParams.ToRenderingParameters())
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to apply group parameters for shader '{0}'", shader.ShaderData.Name);
                }
            }

            // Draw
            try
            {
                gl.BindVertexArray(group.VAO.Handle);

                var geoData = _geometries[group.GeometryId];

                if (geoData.MeshEBO is { } ebo)
                {
                    gl.DrawElementsInstanced(
                        group.PrimitiveType,
                        ebo.IndexCount,
                        DrawElementsType.UnsignedInt,
                        in Unsafe.NullRef<int>(),
                        (uint)instanceCount);
                }
                else
                {
                    gl.DrawArraysInstanced(
                        group.PrimitiveType,
                        0,
                        geoData.VertexCount,
                        (uint)instanceCount);
                }

                gl.BindVertexArray(0);
            }
            catch (Exception e)
            {
                return new ResultProblem(e, "Failed to render group");
            }

            // Restore shader-level uniforms if group overrides were applied
            if (group.GroupParameters is not null)
            {
                if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, shaderParameters)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to restore shader parameters for '{0}'", shader.ShaderData.Name);
                }
            }
        }

        // Restore clean GL state
        gl.DepthMask(true);
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.Blend);

        // Periodic error check
        _errorCounter++;
        if (_errorCounter >= ErrorCounterThreshold)
        {
            var error = gl.GetError();
            if (error != GLEnum.NoError)
            {
                return new ResultProblem("OpenGL error: {0}", error);
            }

            _errorCounter = 0;
        }

        return Result.Success();
    }

    #endregion

    #region Private helpers

    private static void ApplyRenderState(GL gl, RenderState state)
    {
        switch (state.Blend)
        {
            case BlendMode.None:
                gl.Disable(GLEnum.Blend);
                break;
            case BlendMode.Alpha:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Premultiplied:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                break;
        }

        gl.DepthMask(state.DepthWrite);

        if (state.DepthTest)
            gl.Enable(GLEnum.DepthTest);
        else
            gl.Disable(GLEnum.DepthTest);
    }

    private static void RebuildInstanceBuffer(GL gl, GroupData group)
    {
        var floats = group.Instances.MarshalToFloats();

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, group.InstanceVBO.Handle);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, floats, BufferUsageARB.DynamicDraw);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    private static float[] MarshalVertices<T>(ReadOnlySpan<T> vertices) where T : IVertexData
    {
        var floats = new float[vertices.Length * T.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var vertex in vertices)
        {
            vertex.WriteTo(span.Slice(offset, T.FloatCount));
            offset += T.FloatCount;
        }

        return floats;
    }

    #endregion

    #region Internal types

    private sealed record GeometryData(
        VBO MeshVBO,
        EBO? MeshEBO,
        uint VertexCount,
        PrimitiveType PrimitiveType,
        Action<GL>? ConfigureMeshAttributes);

    private sealed class GroupData(
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
        public bool IsDirty { get; private set; } = true; // dirty on creation to upload initial empty buffer

        public void MarkDirty() => IsDirty = true;
        public void ClearDirty() => IsDirty = false;
    }

    private interface IInstanceStore
    {
        int Count { get; }
        float[] MarshalToFloats();
    }

    private sealed class InstanceStore<TInstance> : IInstanceStore where TInstance : IInstanceData
    {
        private readonly Dictionary<Id<TInstance>, TInstance> _instances = new();

        public int Count => _instances.Count;

        public void Add(Id<TInstance> id, TInstance data) => _instances[id] = data;

        public bool Update(Id<TInstance> id, TInstance data)
        {
            if (!_instances.ContainsKey(id)) return false;
            _instances[id] = data;
            return true;
        }

        public bool Remove(Id<TInstance> id) => _instances.Remove(id);

        public float[] MarshalToFloats()
        {
            if (_instances.Count == 0) return [];

            var floats = new float[_instances.Count * TInstance.FloatCount];
            var span = floats.AsSpan();
            var offset = 0;
            foreach (var instance in _instances.Values)
            {
                instance.WriteTo(span.Slice(offset, TInstance.FloatCount));
                offset += TInstance.FloatCount;
            }

            return floats;
        }
    }

    #endregion
}
