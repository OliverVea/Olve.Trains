using Olve.Engine3D.Graphics;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering;

public class ModelRenderingManager : RenderingManagerBase<Model, ModelRenderingManager.ModelRegistration>
{
    public record struct ModelRegistration(OpenGLModelRenderingManager.OpenGLModelRegistration GLModelRegistration, uint VertexCount, uint IndexCount) : IHasInstanceCount
    {
        public int InstanceCount { get; set; } = 0;
    }

    protected override Result<ModelRegistration> RegisterInOpenGL(Model entity)
    {
        if (OpenGLModelRenderingManager.RegisterModelInOpenGL(entity).TryPickProblems(out var problems, out var openglResult))
        {
            return problems.Prepend("Failed to register model in OpenGL");
        }

        return new ModelRegistration(openglResult, (uint)entity.Mesh.Vertices.Length, (uint)entity.Mesh.Indices.Length);
    }

    protected override Result DeregisterFromOpenGL(ModelRegistration registration)
    {
        var result = OpenGLModelRenderingManager.RemoveModelFromOpenGL(registration.GLModelRegistration);
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Could not unregister model in OpenGL");
        }

        return Result.Success();
    }

    protected override Result Load(ModelRegistration registration, RenderingParameters parameters)
    {
        var result = OpenGLModelRenderingManager.LoadModelInOpenGL(registration.GLModelRegistration, parameters);
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load model in OpenGL");
        }

        return Result.Success();
    }

    protected override Result Render(ModelRegistration registration, Matrix4X4<float> world, RenderingParameters parameters)
    {
        var result = OpenGLModelRenderingManager.RenderModel(registration.GLModelRegistration, parameters.WorldMatrixName, world, registration.IndexCount);
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to render model in OpenGL");
        }

        return Result.Success();
    }
}