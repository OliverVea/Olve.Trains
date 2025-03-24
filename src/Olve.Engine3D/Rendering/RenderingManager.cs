using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Parameters;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager
{
    private readonly ThreadSafeUintGenerator _instanceUintGenerator = new();
    private RenderingInstanceId NextInstanceId() => new(_instanceUintGenerator.Next());

    protected readonly List<Instance> Instances = new();

    protected readonly record struct Instance(
        RenderingInstanceId InstanceId,
        MeshRenderingId  MeshId,
        ShaderRenderingId ShaderId,
        Matrix4X4<float> Transform);
    
    public Result<RenderingInstanceId> RegisterInstance(
        MeshRenderingId meshId,
        ShaderRenderingId shaderId,
        Matrix4X4<float> worldMatrix)
    {
        var instanceId = NextInstanceId();
        var instance = new Instance(instanceId, meshId, shaderId, worldMatrix);

        Instances.Add(instance);

        return instanceId;
    }

    public Result DeregisterInstance(RenderingInstanceId instanceId)
    {
        Instances.RemoveAll(x => x.InstanceId == instanceId);

        return Result.Success();
    }

    public Result SetInstanceWorld(RenderingInstanceId instanceId, Matrix4X4<float> worldMatrix)
    {
        var instance = Instances.FirstOrDefault(x => x.InstanceId == instanceId);
        if (instance == default)
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        Instances.RemoveAll(x => x.InstanceId == instanceId);

        instance = instance with { Transform = worldMatrix };

        Instances.Add(instance);

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

    public Result Render(RenderingParameters parameters)
    {
        try
        {
            foreach (var instance in Instances)
            {
                OpenGLModelRenderingManager.OpenGLModelHandles openGLHandles = new(
                    instance.MeshId.VAO,
                    instance.MeshId.VBO,
                    instance.MeshId.EBO,
                    instance.ShaderId.Shader);

                if (OpenGLModelRenderingManager.LoadModelInOpenGL(openGLHandles, parameters)
                    .TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to load model instance into OpenGL");
                }

                if (OpenGLModelRenderingManager.RenderModel(
                        instance.ShaderId.Shader,
                        parameters.WorldMatrixName,
                        instance.Transform,
                        instance.MeshId.EBO.IndexCount).TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to render model instance with OpenGL");
                }
            }
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render entity instances");
        }

        var error = GameManager.GL.GetError();
        if (error != GLEnum.NoError)
        {
            return new ResultProblem("OpenGL error: {0}", error);
        }

        return Result.Success();
    }
}