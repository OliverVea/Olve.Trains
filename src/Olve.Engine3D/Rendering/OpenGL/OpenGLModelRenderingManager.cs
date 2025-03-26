using System.Runtime.CompilerServices;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public static class OpenGLModelRenderingManager
{
    public static Result LoadShaderInOpenGL(ShaderProgram shaderProgram, RenderingParameters parameters)
    {
        GameManager.GL.UseProgram(shaderProgram.Handle);

        var results = parameters.Parameters.Select(parameter => parameter.SetUniforms(shaderProgram));

        if (results.TryPickProblems(out var problems))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }
        }

        return Result.Success();
    }

    public static Result LoadModelInOpenGL(VAO vao, VBO vbo, EBO ebo)
    {
        GameManager.GL.BindVertexArray(vao.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);

        return Result.Success();
    }

    public static Result RenderModel(int worldLocation, int? normalMatrixLocation, Matrix4X4<float> world, uint indexCount)
    {
        Span<float> worldBuffer = stackalloc float[16];
        world.CopyTo(worldBuffer);

        GameManager.GL.UniformMatrix4(worldLocation, 1, false, worldBuffer);

        if (normalMatrixLocation.HasValue)
        {
            Span<float> normalMatrixBuffer = stackalloc float[9];

            var normalMatrix = world.Extract3X3();

            normalMatrix.CopyTo(normalMatrixBuffer);

            GameManager.GL.UniformMatrix3(normalMatrixLocation.Value, 1, true, normalMatrixBuffer);
        }

        return RenderModel(indexCount);
    }

    public static Result RenderModel(uint indexCount)
    {
        GameManager.GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());

        return Result.Success();
    }
}