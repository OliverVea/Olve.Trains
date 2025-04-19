using System.Runtime.CompilerServices;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLModelRenderingManager(Provider<GL> glProvider)
{
    public Result LoadShaderInOpenGL(ShaderProgram shaderProgram, RenderingParameters parameters)
    {
        glProvider.Value.UseProgram(shaderProgram.Handle);

        var results = parameters.Parameters.Select(parameter => parameter.SetUniforms(glProvider.Value, shaderProgram));

        if (results.TryPickProblems(out var problems))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }
        }

        return Result.Success();
    }

    public Result LoadModelInOpenGL(VAO vao, VBO vbo, EBO ebo)
    {
        glProvider.Value.BindVertexArray(vao.Handle);
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);
        glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Handle);

        return Result.Success();
    }

    public Result RenderModel(int worldLocation, int? normalMatrixLocation, Matrix4X4<float> world, uint indexCount)
    {
        Span<float> worldBuffer = stackalloc float[16];
        world.CopyTo(worldBuffer);

        glProvider.Value.UniformMatrix4(worldLocation, 1, false, worldBuffer);

        if (normalMatrixLocation.HasValue)
        {
            Span<float> normalMatrixBuffer = stackalloc float[9];

            var normalMatrix = world.Extract3X3();

            normalMatrix.CopyTo(normalMatrixBuffer);

            glProvider.Value.UniformMatrix3(normalMatrixLocation.Value, 1, true, normalMatrixBuffer);
        }

        return RenderModel(indexCount);
    }

    public Result RenderModel(uint indexCount)
    {
        glProvider.Value.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());

        return Result.Success();
    }
}