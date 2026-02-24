using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLInstancedBufferManager(
    Provider<GL> glProvider,
    OpenGLQuadRenderingManager quadRenderingManager)
{
    public readonly record struct Registration(VAO VAO, VBO InstanceVBO);

    public readonly record struct MeshInstancedRegistration(
        VAO VAO,
        VBO InstanceVBO,
        VBO MeshVBO,
        EBO? MeshEBO);

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

    public Result<MeshInstancedRegistration> CreateMeshInstanceBuffer(
        ReadOnlySpan<float> meshVertexData,
        uint meshVertexCount,
        ReadOnlySpan<uint> meshIndices,
        Action<GL> configureMeshAttributes,
        ReadOnlySpan<float> instanceData,
        uint instanceCount,
        Action<GL> configureInstanceAttributes,
        BufferUsageARB instanceUsage)
    {
        var gl = glProvider.Value;

        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        // Base mesh VBO (per-vertex data, divisor=0)
        var meshVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, meshVboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, meshVertexData, BufferUsageARB.StaticDraw);

        configureMeshAttributes(gl);

        // Base mesh EBO
        EBO? ebo = null;
        if (meshIndices.Length > 0)
        {
            var eboHandle = gl.CreateBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboHandle);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, meshIndices, BufferUsageARB.StaticDraw);
            ebo = new EBO(eboHandle, (uint)meshIndices.Length);
        }

        // Instance VBO (per-instance data, divisor=1)
        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, instanceData, instanceUsage);

        configureInstanceAttributes(gl);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return new MeshInstancedRegistration(
            new VAO(vaoHandle),
            new VBO(instVboHandle, instanceCount),
            new VBO(meshVboHandle, meshVertexCount),
            ebo);
    }

    public void UpdateMeshVertexBuffer(
        MeshInstancedRegistration registration,
        ReadOnlySpan<float> vertexData,
        uint vertexCount,
        BufferUsageARB usage)
    {
        var gl = glProvider.Value;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, registration.MeshVBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertexData, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public void UpdateMeshInstanceBuffer(
        MeshInstancedRegistration registration,
        ReadOnlySpan<float> instanceData,
        uint instanceCount,
        BufferUsageARB usage)
    {
        var gl = glProvider.Value;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, registration.InstanceVBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, instanceData, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public void DeleteMeshInstanceBuffers(MeshInstancedRegistration registration)
    {
        var gl = glProvider.Value;
        gl.DeleteVertexArray(registration.VAO.Handle);
        gl.DeleteBuffer(registration.InstanceVBO.Handle);
        gl.DeleteBuffer(registration.MeshVBO.Handle);
        if (registration.MeshEBO is { } ebo)
            gl.DeleteBuffer(ebo.Handle);
    }

    public void UpdateBuffer(Registration registration, ReadOnlySpan<float> instanceData, BufferUsageARB usage)
    {
        var gl = glProvider.Value;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, registration.InstanceVBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, instanceData, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    public void DeleteBuffers(Registration registration)
    {
        var gl = glProvider.Value;
        gl.DeleteVertexArray(registration.VAO.Handle);
        gl.DeleteBuffer(registration.InstanceVBO.Handle);
    }
}
