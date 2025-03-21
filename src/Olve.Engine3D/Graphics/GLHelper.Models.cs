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

    public static Result LoadModelInOpenGL(OpenGLModelRegistration registrationGLModelRegistration,
        Matrix4X4<float> view, Matrix4X4<float> projection, Ray3D<float> parametersMouseRay,  DirectionalLight parametersDirectionalLight, AmbientLight parametersAmbientLight)
    {
        // Bind VAO, VBO, EBO, and shader program
        GameManager.GL.BindVertexArray(registrationGLModelRegistration.VAO.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, registrationGLModelRegistration.VBO.Handle);
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, registrationGLModelRegistration.EBO.Handle);
        GameManager.GL.UseProgram(registrationGLModelRegistration.ShaderProgram.Handle);

        // Set view
        var viewLocation = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "view");
        Span<float> viewBuffer = stackalloc float[16];
        BufferHelper.CopyTo(view, viewBuffer);
        GameManager.GL.UniformMatrix4(viewLocation, 1, false, viewBuffer);

        // Set projection
        var projectionLocation = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "projection");
        Span<float> projectionBuffer = stackalloc float[16];
        BufferHelper.CopyTo(projection, projectionBuffer);
        GameManager.GL.UniformMatrix4(projectionLocation, 1, false, projectionBuffer);

        // Set mouse ray
        var mouseRayOriginLocation = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "mouseRayOrigin");
        GameManager.GL.Uniform3(mouseRayOriginLocation, parametersMouseRay.Origin.X, parametersMouseRay.Origin.Y, parametersMouseRay.Origin.Z);
        var mouseRayDirectionLocation = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "mouseRayDirection");
        GameManager.GL.Uniform3(mouseRayDirectionLocation, parametersMouseRay.Direction.X, parametersMouseRay.Direction.Y, parametersMouseRay.Direction.Z);

        // Set directional light
        var directionalLightDirection = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "directionalLightDir");
        GameManager.GL.Uniform3(directionalLightDirection, parametersDirectionalLight.Direction.X, parametersDirectionalLight.Direction.Y, parametersDirectionalLight.Direction.Z);
        var directionalLightColor = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "directionalLightColor");
        GameManager.GL.Uniform3(directionalLightColor, parametersDirectionalLight.Color.X, parametersDirectionalLight.Color.Y, parametersDirectionalLight.Color.Z);
        var directionalLightIntensity = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "directionalIntensity");
        GameManager.GL.Uniform1(directionalLightIntensity, parametersDirectionalLight.Intensity);

        // Set ambient light
        var ambientLightColor = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "ambientLightColor");
        GameManager.GL.Uniform3(ambientLightColor, parametersAmbientLight.Color.X, parametersAmbientLight.Color.Y, parametersAmbientLight.Color.Z);
        var ambientLightIntensity = GameManager.GL.GetUniformLocation(registrationGLModelRegistration.ShaderProgram.Handle, "ambientIntensity");
        GameManager.GL.Uniform1(ambientLightIntensity, parametersAmbientLight.Intensity);

        // Enable depth test
        GameManager.GL.Enable(EnableCap.DepthTest);

        return Result.Success();
    }

    public static Result RenderModel(OpenGLModelRegistration heightmapRegistration, Matrix4X4<float> world, uint indexCount)
    {
        // VAO, VBO, EBO, and shader program are already bound

        // Set world
        var worldLocation = GameManager.GL.GetUniformLocation(heightmapRegistration.ShaderProgram.Handle, "world");
        Span<float> worldBuffer = stackalloc float[16];
        BufferHelper.CopyTo(world, worldBuffer);
        GameManager.GL.UniformMatrix4(worldLocation, 1, false, worldBuffer);

        // Draw
        GameManager.GL.DrawElements(PrimitiveType.Triangles, indexCount * 3, DrawElementsType.UnsignedInt, in Unsafe.NullRef<int>());

        return Result.Success();
    }
}