using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager(
    Provider<GL> glProvider,
    OpenGLModelRenderingManager openGLModelRenderingManager,
    MeshEntityManager meshEntityManager,
    HeightmapEntityManager heightmapEntityManager,
    ShaderEntityManager shaderEntityManager)
{
    private readonly ThreadSafeUintGenerator _instanceUintGenerator = new();
    private RenderingInstanceId NextInstanceId() => new(_instanceUintGenerator.Next());

    protected readonly OrderedList<Instance> Instances = new();

    protected readonly record struct Instance(
        RenderingInstanceId InstanceId,
        RenderingId<ShaderData> ShaderId,
        VAO VAO,
        VBO VBO,
        EBO EBO,
        Matrix4X4<float> Transform) : IComparable<Instance>
    {
        public int CompareTo(Instance other)
        {
            // Order: shader > VAO > VBO > EBO > InstanceId

            if (ShaderId.Id != other.ShaderId.Id)
            {
                return ShaderId.Id.CompareTo(other.ShaderId.Id);
            }

            if (VAO.Handle != other.VAO.Handle)
            {
                return VAO.Handle.CompareTo(other.VAO.Handle);
            }

            if (VBO.Handle != other.VBO.Handle)
            {
                return VBO.Handle.CompareTo(other.VBO.Handle);
            }

            if (EBO.Handle != other.EBO.Handle)
            {
                return EBO.Handle.CompareTo(other.EBO.Handle);
            }

            return InstanceId.Id.CompareTo(other.InstanceId.Id);
        }
    }
    
    public Result<RenderingInstanceId> RegisterInstance(
        RenderingId<MeshData> meshId,
        RenderingId<ShaderData> shaderId,
        Matrix4X4<float> worldMatrix)
    {
        if (meshEntityManager.GetRegistration(meshId).TryPickProblems(out var problems, out var meshData))
        {
            return problems.Prepend("Failed to get mesh data");
        }

        if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to get shader data");
        }

        var instanceId = NextInstanceId();
        Instance instance = new(instanceId, shaderId, meshData.VAO, meshData.VBO, meshData.EBO, worldMatrix);

        Instances.Insert(instance);

        return instanceId;
    }

    public Result<RenderingInstanceId> RegisterInstance(
        RenderingId<HeightmapData> terrainId,
        RenderingId<ShaderData> shaderId,
        Matrix4X4<float> worldMatrix)
    {
        if (heightmapEntityManager.GetRegistration(terrainId).TryPickProblems(out var problems, out var terrainRegistration))
        {
            return problems.Prepend("Failed to get mesh data");
        }

        if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to get shader data");
        }

        var instanceId = NextInstanceId();
        Instance instance = new(instanceId, shaderId, terrainRegistration.VAO, terrainRegistration.VBO, terrainRegistration.EBO, worldMatrix);

        Instances.Insert(instance);

        return instanceId;
    }

    public Result DeregisterInstance(RenderingInstanceId instanceId)
    {
        var instance = Instances.FirstOrDefault(x => x.InstanceId == instanceId);

        if (instance == default)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        Instances.Remove(instance);

        return Result.Success();
    }

    public Result SetInstanceWorld(RenderingInstanceId instanceId, Matrix4X4<float> worldMatrix)
    {
        var instance = Instances.FirstOrDefault(x => x.InstanceId == instanceId);
        if (instance == default)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        var newInstance = instance with { Transform = worldMatrix };

        Instances.Replace(newInstance);

        return Result.Success();
    }

    public Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId instanceId)
    {
        var instance = Instances.FirstOrDefault(x => x.InstanceId == instanceId);
        if (instance == default)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        return instance.Transform;
    }

    public Result Render(IShader shader)
    {
        if (shader.RenderingId.Id == 0)
        {
            return new ResultProblem("Shader ID is not set");
        }

        var startIndex = Instances.GetIndex(new Instance { ShaderId = shader.RenderingId });
        var endIndex = Instances.GetIndex(new Instance { ShaderId = new RenderingId<ShaderData>(shader.RenderingId.Id + 1) });

        if (startIndex == endIndex)
        {
            return Result.Success();
        }

        var instanceRange = Instances.GetRange(startIndex, endIndex - startIndex);

        if (shaderEntityManager.GetRegistration(shader.RenderingId)
            .TryPickProblems(out var problems, out var shaderRegistration))
        {
            return problems.Prepend("Failed to get shader registration for shader '{0}' ('{1}').", shader.ShaderData.Name, shader.RenderingId);
        }

        var parameters = shader.MakeParameters();

        if (openGLModelRenderingManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
        }

        if (RenderInstances(instanceRange, shaderRegistration).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to render entity instances with shader '{0}'", shader.ShaderData.Name);
        }

        return Result.Success();
    }

    private Result RenderInstances(IEnumerable<Instance> instanceRange, OpenGLShaderManager.Registration shaderRegistration)
    {
        try
        {
            foreach (var instance in instanceRange)
            {
                if (openGLModelRenderingManager.LoadModelInOpenGL(
                        instance.VAO,
                        instance.VBO,
                        instance.EBO).TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to load model instance into OpenGL");
                }

                if (shaderRegistration.WorldPositionLocation is { } worldPositionLocation)
                {
                    if (openGLModelRenderingManager.RenderModel(
                            worldPositionLocation,
                            shaderRegistration.NormalMatrixLocation,
                            instance.Transform,
                            instance.EBO.IndexCount).TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to render model instance with OpenGL");
                    }
                }
                else
                {
                    if (openGLModelRenderingManager.RenderModel(instance.EBO.IndexCount).TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to render model instance with OpenGL");
                    }
                }
            }
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render entity instances");
        }

        var error = glProvider.Value.GetError();
        if (error != GLEnum.NoError)
        {
            return new ResultProblem("OpenGL error: {0}", error);
        }

        return Result.Success();
    }
}