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

public class RenderingManager3D(
    Provider<GL> glProvider,
    OpenGLShaderManager openGLShaderManager,
    OpenGLBufferManager bufferManager,
    ShaderEntityManager shaderEntityManager)
{
    private RenderingInstanceId NextInstanceId() => new(Id.New());

    // Dictionary on shader?
    protected readonly SortedList<RenderingInstanceId, Instance> Instances = new();

    private readonly Dictionary<GeometryId, GeometryRegistration> _geometries = new();

    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    private readonly record struct GeometryRegistration(
        OpenGLBufferManager.Registration BufferRegistration,
        PrimitiveType PrimitiveType);

    protected readonly record struct Instance(
        RenderingInstanceId InstanceId,
        RenderingId<ShaderData> ShaderId,
        GeometryId GeometryId,
        Matrix4X4<float> Transform,
        IShaderParameters? Parameters);

    public Result<GeometryId> RegisterGeometry<T>(
        ReadOnlySpan<T> vertices, ReadOnlySpan<uint> indices) where T : IVertexData
    {
        return RegisterGeometry(vertices, indices, PrimitiveType.Triangles);
    }

    public Result<GeometryId> RegisterGeometry<T>(
        ReadOnlySpan<T> vertices, ReadOnlySpan<uint> indices, PrimitiveType primitiveType,
        BufferUsageARB usage = BufferUsageARB.StaticDraw) where T : IVertexData
    {

        // TODO: investigate this
        var floatCount = vertices.Length * T.FloatCount;
        var floats = new float[floatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var vertex in vertices)
        {
            vertex.WriteTo(span.Slice(offset, T.FloatCount));
            offset += T.FloatCount;
        }

        var reg = bufferManager.CreateBuffers(
            floats,
            (uint)vertices.Length,
            indices,
            T.ConfigureAttributes,
            usage);

        var id = GeometryId.New();
        _geometries[id] = new GeometryRegistration(reg, primitiveType);
        return id;
    }

    public Result<GeometryId> RegisterGeometry<T>(
        ReadOnlySpan<T> vertices, PrimitiveType primitiveType,
        BufferUsageARB usage = BufferUsageARB.StaticDraw) where T : IVertexData
    {
        return RegisterGeometry(vertices, ReadOnlySpan<uint>.Empty, primitiveType, usage);
    }

    public Result<GeometryId> RegisterDrawArraysGeometry(
        uint vertexCount, PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        var reg = bufferManager.CreateDrawArraysBuffers(vertexCount);
        var id = GeometryId.New();
        _geometries[id] = new GeometryRegistration(reg, primitiveType);
        return id;
    }

    public Result UpdateGeometry<T>(GeometryId geometryId, ReadOnlySpan<T> vertices) where T : IVertexData
    {
        if (!_geometries.TryGetValue(geometryId, out var geoReg))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        var floatCount = vertices.Length * T.FloatCount;
        var floats = new float[floatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var vertex in vertices)
        {
            vertex.WriteTo(span.Slice(offset, T.FloatCount));
            offset += T.FloatCount;
        }

        bufferManager.UpdateVBO(geoReg.BufferRegistration, floats, BufferUsageARB.DynamicDraw);

        // Update vertex count
        var updatedBufReg = geoReg.BufferRegistration with
        {
            VBO = new VBO(geoReg.BufferRegistration.VBO.Handle, (uint)vertices.Length)
        };
        _geometries[geometryId] = geoReg with { BufferRegistration = updatedBufReg };

        return Result.Success();
    }

    public Result<RenderingInstanceId> RegisterInstance(
        GeometryId geometryId,
        RenderingId<ShaderData> shaderId,
        Matrix4X4<float> worldMatrix)
    {
        if (!_geometries.ContainsKey(geometryId))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out var problems, out _))
        {
            return problems.Prepend("Failed to get shader data");
        }

        var instanceId = NextInstanceId();
        Instance instance = new(instanceId, shaderId, geometryId, worldMatrix, null);

        Instances.Add(instanceId, instance);

        return instanceId;
    }

    public Result DeregisterInstance(RenderingInstanceId instanceId)
    {
        if (!Instances.Remove(instanceId))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        return Result.Success();
    }

    public Result DeregisterGeometry(GeometryId geometryId)
    {
        if (!_geometries.Remove(geometryId, out var geoReg))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        bufferManager.DeleteBuffers(geoReg.BufferRegistration);
        return Result.Success();
    }

    public Result SetInstanceWorld(RenderingInstanceId instanceId, Matrix4X4<float> worldMatrix)
    {
        var instanceIndex = Instances.IndexOfKey(instanceId);
        if (instanceIndex == -1)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        var instance = Instances.GetValueAtIndex(instanceIndex);
        var newInstance = instance with { Transform = worldMatrix };

        Instances.SetValueAtIndex(instanceIndex, newInstance);

        return Result.Success();
    }

    public Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId instanceId)
    {
        if (!Instances.TryGetValue(instanceId, out var instance))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        return instance.Transform;
    }

    public Result SetInstanceParameters(RenderingInstanceId instanceId, IShaderParameters? parameters)
    {
        var instanceIndex = Instances.IndexOfKey(instanceId);
        if (instanceIndex == -1)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        var instance = Instances.GetValueAtIndex(instanceIndex);
        Instances.SetValueAtIndex(instanceIndex, instance with { Parameters = parameters });

        return Result.Success();
    }

    public Result Render(IShader shader)
    {
        if (shader.RenderingId == default)
        {
            return new ResultProblem("Shader ID is not set");
        }

        if (Instances.Count == 0)
        {
            return Result.Success();
        }

        if (shaderEntityManager.GetRegistration(shader.RenderingId)
            .TryPickProblems(out var problems, out var shaderRegistration))
        {
            return problems.Prepend("Failed to get shader registration for shader '{0}' ('{1}').", shader.ShaderData.Name, shader.RenderingId);
        }

        switch (shader.BlendState.Blend)
        {
            case BlendMode.None:
                glProvider.Value.Disable(GLEnum.Blend);
                break;
            case BlendMode.Alpha:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Premultiplied:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.One); // common additive
                break;
        }

        glProvider.Value.DepthMask(shader.BlendState.DepthWrite);

        if (!shader.BlendState.DepthTest)
        {
            glProvider.Value.Disable(GLEnum.DepthTest);
        }

        var parameters = shader.MakeParameters();

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
        }

        if (RenderInstances(shader.RenderingId, shaderRegistration, parameters).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to render entity instances with shader '{0}'", shader.ShaderData.Name);
        }

        glProvider.Value.DepthMask(true);
        glProvider.Value.Disable(GLEnum.Blend);

        if (!shader.BlendState.DepthTest)
        {
            glProvider.Value.Enable(GLEnum.DepthTest);
        }

        return Result.Success();
    }

    private Result RenderInstances(RenderingId<ShaderData> shaderRenderingId,
        OpenGLShaderManager.Registration shaderRegistration, RenderingParameters shaderParameters)
    {
        Span<float> worldBuffer = stackalloc float[16];
        Span<float> normalMatrixBuffer = stackalloc float[9];
        try
        {
            foreach (var instance in Instances.Values)
            {
                if (instance.ShaderId != shaderRenderingId)
                {
                    continue;
                }

                if (!_geometries.TryGetValue(instance.GeometryId, out var geoReg))
                {
                    return new ResultProblem("Geometry with id '{0}' is not registered", instance.GeometryId);
                }

                var bufReg = geoReg.BufferRegistration;

                // Apply per-instance shader parameters if present
                if (instance.Parameters is { } entityParams)
                {
                    if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, entityParams.ToRenderingParameters())
                        .TryPickProblems(out var paramProblems))
                    {
                        return paramProblems.Prepend("Failed to apply entity shader parameters");
                    }
                }

                glProvider.Value.BindVertexArray(bufReg.VAO.Handle);
                if (bufReg.VBO.Handle != 0)
                    glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, bufReg.VBO.Handle);

                if (shaderRegistration.WorldPositionLocation is { } worldPositionLocation)
                {
                    instance.Transform.CopyTo(worldBuffer);
                    glProvider.Value.UniformMatrix4(worldPositionLocation, 1, false, worldBuffer);

                    if (shaderRegistration.NormalMatrixLocation is { } normalMatrixLocation)
                    {
                        instance.Transform.Extract3X3().CopyTo(normalMatrixBuffer);
                        glProvider.Value.UniformMatrix3(normalMatrixLocation, 1, true, normalMatrixBuffer);
                    }
                }

                if (bufReg.EBO is { } ebo)
                {
                    glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);
                    glProvider.Value.DrawElements(geoReg.PrimitiveType, ebo.IndexCount, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());
                }
                else
                {
                    glProvider.Value.DrawArrays(geoReg.PrimitiveType, 0, bufReg.VBO.VertexCount);
                }

                glProvider.Value.BindVertexArray(0);
                if (bufReg.VBO.Handle != 0)
                    glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

                // Restore shader-level parameters if per-instance overrides were applied
                if (instance.Parameters is not null)
                {
                    if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, shaderParameters)
                        .TryPickProblems(out var restoreProblems))
                    {
                        return restoreProblems.Prepend("Failed to restore shader-level parameters");
                    }
                }
            }
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render entity instances");
        }

        _errorCounter++;
        if (_errorCounter >= ErrorCounterThreshold)
        {
            var error = glProvider.Value.GetError();
            if (error != GLEnum.NoError)
            {
                return new ResultProblem("OpenGL error: {0}", error);
            }

            _errorCounter = 0;
        }

        return Result.Success();
    }

    public Result RenderInstanced(
        IShader shader,
        OpenGLInstancedBufferManager.MeshInstancedRegistration registration,
        uint instanceCount,
        PrimitiveType primitiveType,
        uint? vertexCount = null,
        IShaderParameters? groupParameters = null)
    {
        return RenderInstancedCore(shader, registration, instanceCount, primitiveType,
            vertexCount ?? registration.MeshVBO.VertexCount, groupParameters);
    }

    public Result RenderInstanced(
        IShader shader,
        OpenGLInstancedBufferManager.MeshInstancedRegistration registration,
        uint instanceCount)
    {
        return RenderInstancedCore(shader, registration, instanceCount, PrimitiveType.Triangles,
            registration.MeshVBO.VertexCount, null);
    }

    private Result RenderInstancedCore(
        IShader shader,
        OpenGLInstancedBufferManager.MeshInstancedRegistration registration,
        uint instanceCount,
        PrimitiveType primitiveType,
        uint vertexCount,
        IShaderParameters? groupParameters)
    {
        if (instanceCount == 0)
        {
            return Result.Success();
        }

        if (shader.RenderingId == default)
        {
            return new ResultProblem("Shader ID is not set");
        }

        if (shaderEntityManager.GetRegistration(shader.RenderingId)
            .TryPickProblems(out var problems, out var shaderRegistration))
        {
            return problems.Prepend("Failed to get shader registration for shader '{0}' ('{1}').",
                shader.ShaderData.Name, shader.RenderingId);
        }

        switch (shader.BlendState.Blend)
        {
            case BlendMode.None:
                glProvider.Value.Disable(GLEnum.Blend);
                break;
            case BlendMode.Alpha:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Premultiplied:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                glProvider.Value.Enable(GLEnum.Blend);
                glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                break;
        }

        glProvider.Value.DepthMask(shader.BlendState.DepthWrite);

        if (!shader.BlendState.DepthTest)
        {
            glProvider.Value.Disable(GLEnum.DepthTest);
        }

        var shaderParameters = shader.MakeParameters();

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, shaderParameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
        }

        // Apply per-group uniform overrides
        if (groupParameters is not null)
        {
            if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, groupParameters.ToRenderingParameters())
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to apply group parameters");
            }
        }

        try
        {
            glProvider.Value.BindVertexArray(registration.VAO.Handle);

            if (registration.MeshEBO is { } ebo)
            {
                glProvider.Value.DrawElementsInstanced(
                    primitiveType,
                    ebo.IndexCount,
                    DrawElementsType.UnsignedInt,
                    in Unsafe.NullRef<int>(),
                    instanceCount);
            }
            else
            {
                glProvider.Value.DrawArraysInstanced(
                    primitiveType,
                    0,
                    vertexCount,
                    instanceCount);
            }

            glProvider.Value.BindVertexArray(0);
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render instanced mesh");
        }

        // Restore shader-level uniforms if group overrides were applied
        if (groupParameters is not null)
        {
            if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, shaderParameters)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to restore shader-level parameters");
            }
        }

        glProvider.Value.DepthMask(true);
        glProvider.Value.Disable(GLEnum.Blend);

        if (!shader.BlendState.DepthTest)
        {
            glProvider.Value.Enable(GLEnum.DepthTest);
        }

        return Result.Success();
    }
}
