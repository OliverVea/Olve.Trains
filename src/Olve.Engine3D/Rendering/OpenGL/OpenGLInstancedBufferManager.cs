using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLInstancedBufferManager(
    Provider<GL> glProvider,
    OpenGLQuadRenderingManager quadRenderingManager)
{
    public readonly record struct Registration(VAO VAO, VBO InstanceVBO);

    public Result<Registration> CreateInstanceBuffer(
        ReadOnlySpan<float> instanceData,
        Action<GL> configureInstanceAttributes,
        BufferUsageARB usage)
    {
        var gl = glProvider.Value;

        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        if (quadRenderingManager.AttachUnitQuad(new VAO(vaoHandle)).TryPickProblems(out var problems))
        {
            gl.BindVertexArray(0);
            return problems.Prepend("Failed to attach unit quad");
        }

        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, instanceData, usage);

        configureInstanceAttributes(gl);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return new Registration(new VAO(vaoHandle), new VBO(instVboHandle, 6));
    }

    public void DeleteBuffers(Registration registration)
    {
        var gl = glProvider.Value;
        gl.DeleteVertexArray(registration.VAO.Handle);
        gl.DeleteBuffer(registration.InstanceVBO.Handle);
    }
}
