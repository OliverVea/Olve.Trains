using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLBufferManager(Provider<GL> glProvider)
{
    public readonly record struct Registration(VAO VAO, VBO VBO, EBO? EBO);

    public Registration CreateBuffers(
        ReadOnlySpan<float> vertexData,
        uint vertexCount,
        ReadOnlySpan<uint> indices,
        Action<GL> configureAttributes,
        BufferUsageARB usage)
    {
        var gl = glProvider.Value;

        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        var vboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertexData, usage);

        EBO? ebo = null;
        if (indices.Length > 0)
        {
            var eboHandle = gl.CreateBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboHandle);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, usage);
            ebo = new EBO(eboHandle, (uint)indices.Length);
        }

        configureAttributes(gl);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        if (ebo.HasValue)
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        return new Registration(new VAO(vaoHandle), new VBO(vboHandle, vertexCount), ebo);
    }

    public Registration CreateBuffers(
        ReadOnlySpan<float> vertexData,
        uint vertexCount,
        Action<GL> configureAttributes,
        BufferUsageARB usage)
    {
        return CreateBuffers(vertexData, vertexCount, ReadOnlySpan<uint>.Empty, configureAttributes, usage);
    }

    public void UpdateVBO(Registration registration, ReadOnlySpan<float> vertexData, BufferUsageARB usage)
    {
        var gl = glProvider.Value;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, registration.VBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertexData, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public void DeleteBuffers(Registration registration)
    {
        var gl = glProvider.Value;
        gl.DeleteVertexArray(registration.VAO.Handle);
        gl.DeleteBuffer(registration.VBO.Handle);
        if (registration.EBO is { } ebo)
            gl.DeleteBuffer(ebo.Handle);
    }
}
