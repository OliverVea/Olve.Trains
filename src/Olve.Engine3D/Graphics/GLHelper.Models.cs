using System.Buffers;
using System.Runtime.CompilerServices;
using Olve.Engine3D.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Graphics;

public static partial class GLHelper
{
    private const int MaxStackAllocationSize = 2048;
    private const int FieldsPerVertex = 6; // position (3) + normal(3)

    public readonly record struct OpenGLModelRegistration(VAO VAO, VBO VBO, EBO EBO, ShaderProgram ShaderProgram);

    public static Result<OpenGLModelRegistration> RegisterModelInOpenGL(Model model)
    {
        // Bind VAO
        var vao = GameManager.GL.CreateVertexArray();
        GameManager.GL.BindVertexArray(vao);

        // Bind VBO
        var vbo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        var verticesLength = model.Vertices.Length * FieldsPerVertex;
        var array = verticesLength > MaxStackAllocationSize ? ArrayPool<float>.Shared.Rent(verticesLength) : null;
        var vertices = verticesLength > MaxStackAllocationSize ? array![..verticesLength] : stackalloc float[model.Vertices.Length * FieldsPerVertex];
        BufferHelper.CopyTo(model.Vertices, vertices, FieldsPerVertex, offset: 0);
        BufferHelper.CopyTo(model.Normals, vertices, FieldsPerVertex, offset: 3);
        GameManager.GL.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        if (array is not null)
        {
            ArrayPool<float>.Shared.Return(array);
        }

        // Bind EBO
        var ebo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        var indicesLength = model.Indices.Length * 3;
        var array2 = indicesLength > MaxStackAllocationSize ? ArrayPool<uint>.Shared.Rent(indicesLength) : null;
        var indices = indicesLength > MaxStackAllocationSize ? array2![..indicesLength] : stackalloc uint[model.Indices.Length * 3];
        BufferHelper.CopyTo(model.Indices, indices);
        GameManager.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        if (array2 is not null)
        {
            ArrayPool<uint>.Shared.Return(array2);
        }

        // Set vertex attributes
        GameManager.GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), IntPtr.Zero);
        GameManager.GL.EnableVertexAttribArray(0);

        GameManager.GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), IntPtr.Zero + sizeof(float) * 3);
        GameManager.GL.EnableVertexAttribArray(1);

        // Load shader
        var vertexShader = GameManager.GL.CreateShader(ShaderType.VertexShader);
        GameManager.GL.ShaderSource(vertexShader, model.ShaderData.VertexShaderSource.SourceCode);
        GameManager.GL.CompileShader(vertexShader);
        GameManager.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);

        if (vStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Vertex shader '{0}' failed to compile with message '{1}'",
                model.ShaderData.VertexShaderSource.Path,
                GameManager.GL.GetShaderInfoLog(vertexShader));
        }

        var fragmentShader = GameManager.GL.CreateShader(ShaderType.FragmentShader);
        GameManager.GL.ShaderSource(fragmentShader, model.ShaderData.FragmentShaderSource.SourceCode);
        GameManager.GL.CompileShader(fragmentShader);
        GameManager.GL.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var fStatus);

        if (fStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Fragment shader '{0}' failed to compile with message '{1}'",
                model.ShaderData.FragmentShaderSource.Path,
                GameManager.GL.GetShaderInfoLog(fragmentShader));
        }

        ShaderProgram shaderProgram = new(GameManager.GL.CreateProgram());
        GameManager.GL.AttachShader(shaderProgram.Handle, vertexShader);
        GameManager.GL.AttachShader(shaderProgram.Handle, fragmentShader);

        GameManager.GL.LinkProgram(shaderProgram.Handle);
        GameManager.GL.GetProgram(shaderProgram.Handle, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Shader program failed to link with message: '{0}'",
                GameManager.GL.GetProgramInfoLog(shaderProgram.Handle));
        }

        GameManager.GL.DetachShader(shaderProgram.Handle, vertexShader);
        GameManager.GL.DetachShader(shaderProgram.Handle, fragmentShader);
        GameManager.GL.DeleteShader(vertexShader);
        GameManager.GL.DeleteShader(fragmentShader);

        // Unbind VAO
        GameManager.GL.BindVertexArray(0);

        // Unbind VBO
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // Unbind EBO
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        return new OpenGLModelRegistration(
            new VAO(vao), new VBO(vbo, (uint)model.Vertices.Length), new EBO(ebo, (uint)indicesLength), shaderProgram);
    }

    public static Result RemoveModelFromOpenGL(OpenGLModelRegistration modelRegistration)
    {
        // Delete VAO, VBO, EBO, and shader program
        GameManager.GL.DeleteVertexArray(modelRegistration.VAO.Handle);
        GameManager.GL.DeleteBuffer(modelRegistration.VBO.Handle);
        GameManager.GL.DeleteBuffer(modelRegistration.EBO.Handle);
        GameManager.GL.DeleteProgram(modelRegistration.ShaderProgram.Handle);

        return Result.Success();
    }

    public static Result LoadModelInOpenGL(OpenGLModelRegistration registrationGLModelRegistration, RenderingParameters parameters)
    {
        // Bind VAO, VBO, EBO, and shader program
        GameManager.GL.BindVertexArray(registrationGLModelRegistration.VAO.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, registrationGLModelRegistration.VBO.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, registrationGLModelRegistration.EBO.Handle);
        GameManager.GL.UseProgram(registrationGLModelRegistration.ShaderProgram.Handle);

        foreach (var parameter in parameters.Parameters)
        {
            if (parameter.ApplyRenderingParameter(registrationGLModelRegistration.ShaderProgram).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to apply rendering parameter");
            }
        }

        // Enable depth test
        GameManager.GL.Enable(EnableCap.DepthTest);

        return Result.Success();
    }

    public static Result RenderModel(OpenGLModelRegistration heightmapRegistration, string? worldName, Matrix4X4<float> world, uint indexCount)
    {
        // VAO, VBO, EBO, and shader program are already bound

        // Set world
        if (worldName is not null)
        {
            var worldLocation = GameManager.GL.GetUniformLocation(heightmapRegistration.ShaderProgram.Handle, worldName);
            Span<float> worldBuffer = stackalloc float[16];
            BufferHelper.CopyTo(world, worldBuffer);
            GameManager.GL.UniformMatrix4(worldLocation, 1, false, worldBuffer);
        }

        // Draw
        GameManager.GL.DrawElements(PrimitiveType.Triangles, indexCount * 3, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());

        return Result.Success();
    }
}

public static class RenderingParameterHelper
{
    public static Result ApplyRenderingParameter(this AnyRenderingParameter renderingParameter, ShaderProgram shaderProgram)
    {
        var location = GameManager.GL.GetUniformLocation(shaderProgram.Handle, renderingParameter.Name);
        if (location == -1)
        {
            return new ResultProblem("Could not find location for rendering parameter '{0}'", renderingParameter.Name);
        }

        return renderingParameter.Match(
            x => ApplyMatrix4X4(x, location),
            x => ApplyVector3D(x, location),
            x => ApplyFloat(x, location));
    }

    private static Result ApplyMatrix4X4(RenderingParameter.Matrix4X4 matrix, int location)
    {
        Span<float> buffer = stackalloc float[16];
        BufferHelper.CopyTo(matrix.Value, buffer);
        GameManager.GL.UniformMatrix4(location, 1, false, buffer);
        return Result.Success();
    }

    private static Result ApplyVector3D(RenderingParameter.Vector3D vector, int location)
    {
        GameManager.GL.Uniform3(location, vector.Value.X, vector.Value.Y, vector.Value.Z);
        return Result.Success();
    }

    private static Result ApplyFloat(RenderingParameter.Float f, int location)
    {
        GameManager.GL.Uniform1(location, f.Value);
        return Result.Success();
    }
}