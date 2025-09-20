using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager3D(
    Provider<GL> glProvider,
    OpenGLModelRenderingManager openGLModelRenderingManager,
    OpenGLShaderManager openGLShaderManager,
    MeshEntityManager meshEntityManager,
    HeightmapEntityManager heightmapEntityManager,
    ShaderEntityManager shaderEntityManager)
{
    private readonly ThreadSafeUintGenerator _instanceUintGenerator = new();
    private RenderingInstanceId NextInstanceId() => new(_instanceUintGenerator.Next());

    // Dictionary on shader?
    protected readonly SortedList<RenderingInstanceId, Instance> Instances = new();

    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    protected readonly record struct Instance(
        RenderingInstanceId InstanceId,
        RenderingId<ShaderData> ShaderId,
        VAO VAO,
        VBO VBO,
        EBO EBO,
        Matrix4X4<float> Transform);
    
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

        Instances.Add(instanceId, instance);

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

    public Result Render(IShader shader)
    {
        if (shader.RenderingId.Id == 0)
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

        var parameters = shader.MakeParameters();

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
        }

        if (RenderInstances(shader.RenderingId, shaderRegistration).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to render entity instances with shader '{0}'", shader.ShaderData.Name);
        }   
        
        glProvider.Value.DepthMask(true);
        glProvider.Value.Disable(GLEnum.Blend);

        return Result.Success();
    }

    private Result RenderInstances(RenderingId<ShaderData> shaderRenderingId,
        OpenGLShaderManager.Registration shaderRegistration)
    {
        try
        {
            foreach (var instance in Instances.Values)
            {
                if (instance.ShaderId != shaderRenderingId)
                {
                    continue;
                }
                
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
}