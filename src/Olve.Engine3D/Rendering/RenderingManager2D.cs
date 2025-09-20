using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public class RenderingManager2D(
    Provider<GL> glProvider,
    OpenGLQuadRenderingManager openGLQuadRenderingManager,
    OpenGLRectangleManager rectangleGlManager,
    OpenGLShaderManager openGLShaderManager,
    ShaderEntityManager shaderEntityManager)
{
    private readonly ThreadSafeUintGenerator _instanceUintGenerator = new();
    private RenderingInstanceId NextInstanceId() => new(_instanceUintGenerator.Next());

    protected readonly SortedList<RenderingInstanceId, Instance> Instances = new();

    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    // Each instance = one rectangle for a given shader
    protected readonly record struct Instance(
        RenderingInstanceId InstanceId,
        RenderingId<ShaderData> ShaderId,
        VAO VAO,
        VBO InstanceVBO,
        uint InstanceCount);

    // ---------- Instance lifecycle ----------

    public Result<RenderingInstanceId> RegisterRectangle(
        RenderingId<ShaderData> shaderId,
        RectangleData rectangle)
    {
        // Validate rectangle and shader availability
        if (rectangle.Validate().TryPickProblems(out var problems))
            return problems.Prepend("Invalid RectangleData");

        if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out problems, out _))
            return problems.Prepend("Failed to get shader data");

        // Create VAO + per-instance VBO (and attach shared unit-quad @ attrib 0 inside)
        if (rectangleGlManager.Register(rectangle).TryPickProblems(out problems, out var reg))
            return problems.Prepend("Failed to create OpenGL registration for rectangle");

        var id = NextInstanceId();
        var instance = new Instance(id, shaderId, reg.VAO, reg.InstanceVBO, 1);
        Instances.Add(id, instance);

        return id;
    }

    public Result DeregisterRectangle(RenderingInstanceId instanceId)
    {
        var idx = Instances.IndexOfKey(instanceId);
        if (idx == -1)
            return new ResultProblem("GUI rectangle with id '{0}' is not registered", instanceId);

        var inst = Instances.GetValueAtIndex(idx);
        var _ = rectangleGlManager.Unregister(new OpenGLRectangleManager.Registration(inst.VAO, inst.InstanceVBO));

        Instances.RemoveAt(idx);
        return Result.Success();
    }

    public Result UpdateRectangle(RenderingInstanceId instanceId, RectangleData rectangle)
    {
        if (rectangle.Validate().TryPickProblems(out var problems))
            return problems.Prepend("Invalid RectangleData");

        var idx = Instances.IndexOfKey(instanceId);
        if (idx == -1)
            return new ResultProblem("GUI rectangle with id '{0}' is not registered", instanceId);

        // Simple approach: re-register GPU resources for this rectangle
        var old = Instances.GetValueAtIndex(idx);
        rectangleGlManager.Unregister(new OpenGLRectangleManager.Registration(old.VAO, old.InstanceVBO));

        if (rectangleGlManager.Register(rectangle).TryPickProblems(out problems, out var reg))
            return problems.Prepend("Failed to upload updated rectangle data");

        Instances.SetValueAtIndex(idx, old with { VAO = reg.VAO, InstanceVBO = reg.InstanceVBO });
        return Result.Success();
    }

    // ---------- Render pass ----------

    public Result Render(IShader shader)
    {
        if (shader.RenderingId.Id == 0)
            return new ResultProblem("Shader ID is not set");

        if (Instances.Count == 0)
            return Result.Success();

        if (shaderEntityManager.GetRegistration(shader.RenderingId)
            .TryPickProblems(out var problems, out var shaderRegistration))
        {
            return problems.Prepend("Failed to get shader registration for shader '{0}' ('{1}').",
                shader.ShaderData.Name, shader.RenderingId);
        }

        // Blend/depth state (UI typically Alpha blend and DepthWrite=false)
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

        var parameters = shader.MakeParameters(); // Expect your shader to include uResolution etc.

        if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
        }

        if (RenderInstances(shader.RenderingId).TryPickProblems(out problems))
            return problems.Prepend("Failed to render GUI rectangles with shader '{0}'", shader.ShaderData.Name);

        // Restore state
        glProvider.Value.DepthMask(true);
        glProvider.Value.Disable(GLEnum.Blend);

        return Result.Success();
    }

    private Result RenderInstances(RenderingId<ShaderData> shaderRenderingId)
    {
        try
        {
            foreach (var inst in Instances.Values)
            {
                if (inst.ShaderId != shaderRenderingId)
                    continue;

                // Bind VAO (contains aUnit @ 0 + instance attrs @ 1..6) and instance VBO
                if (openGLQuadRenderingManager.LoadQuadsInOpenGL(inst.VAO, inst.InstanceVBO)
                    .TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to bind rectangle instance");
                }

                // Draw one quad for this instance (if you later batch, pass >1)
                if (openGLQuadRenderingManager.RenderQuads(inst.InstanceCount)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to draw rectangle instance");
                }

                // Unbind VAO
                glProvider.Value.BindVertexArray(0);
            }
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render GUI rectangles");
        }

        // Periodic GL error check
        _errorCounter++;
        if (_errorCounter >= ErrorCounterThreshold)
        {
            var error = glProvider.Value.GetError();
            _errorCounter = 0;
            if (error != GLEnum.NoError)
                return new ResultProblem("OpenGL error: {0}", error);
        }

        return Result.Success();
    }
}
