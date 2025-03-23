using System.Runtime.CompilerServices;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Rendering.OpenGL.Types;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLModelRenderingManager
{
    public readonly record struct OpenGLModelHandles(
        VAO VAO,
        VBO VBO,
        EBO EBO,
        ShaderProgram ShaderProgram);

    public static Result LoadModelInOpenGL(OpenGLModelHandles modelHandles, RenderingParameters parameters)
    {
        var (vao, vbo, ebo, shaderProgram) = modelHandles;

        // Bind VAO, VBO, EBO, and shader program
        GameManager.GL.BindVertexArray(vao.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);
        GameManager.GL.UseProgram(shaderProgram.Handle);

        foreach (var parameter in parameters.Parameters)
        {
            if (parameter.ApplyRenderingParameter(shaderProgram).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to apply rendering parameter");
            }
        }

        // Enable depth test
        GameManager.GL.Enable(EnableCap.DepthTest);

        return Result.Success();
    }

    public static Result RenderModel(OpenGLModelHandles glModelHandles, string? worldName, Matrix4X4<float> world, uint indexCount)
    {
        // VAO, VBO, EBO, and shader program are already bound

        // Set world
        if (worldName is not null)
        {
            // TODO: Cache uniform location
            var worldLocation = GameManager.GL.GetUniformLocation(glModelHandles.ShaderProgram.Handle, worldName);
            Span<float> worldBuffer = stackalloc float[16];
            BufferHelper.CopyTo(world, worldBuffer);
            GameManager.GL.UniformMatrix4(worldLocation, 1, false, worldBuffer);
        }

        // Draw
        GameManager.GL.DrawElements(PrimitiveType.Triangles, indexCount * 3, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());

        return Result.Success();
    }
}