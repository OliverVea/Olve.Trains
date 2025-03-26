using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLShaderManager : IOpenGLEntityManager<ShaderData, OpenGLShaderManager.Registration>
{
    public readonly record struct Registration(
        ShaderProgram ShaderProgram,
        int? WorldPositionLocation,
        int? NormalMatrixLocation);

    public Result<Registration> Register(ShaderData shaderData)
    {
        if (LoadShader(shaderData.VertexSource, ShaderType.VertexShader)
            .TryPickProblems(out var problems, out var vertexShader))
        {
            return problems.Prepend("Vertex shader for shader '{0}' could not be loaded.", shaderData.Name);
        }

        if (LoadShader(shaderData.FragmentSource, ShaderType.FragmentShader)
            .TryPickProblems(out problems, out var fragmentShader))
        {
            return problems.Prepend("Fragment shader for shader '{0}' could not be loaded.", shaderData.Name);
        }

        List<uint> shaderPrograms = [vertexShader, fragmentShader];

        if (shaderData.GeometrySource is { } geometrySource)
        {
            if (LoadShader(geometrySource, ShaderType.GeometryShader)
                .TryPickProblems(out problems, out var geometryShader))
            {
                return problems.Prepend("Geometry shader for shader '{0}' could not be loaded.", shaderData.Name);
            }

            shaderPrograms.Add(geometryShader);
        }

        if (CreateShaderProgram(shaderPrograms)
            .TryPickProblems(out problems, out var shaderProgram))
        {
            return problems.Prepend("Shader program '{0}' could not be created", shaderData.Name);
        }

        if (GetUniformLocation(shaderProgram, "world")
            .TryPickProblems(out problems, out var worldPositionLocation))
        {
            return problems.Prepend("World position location for shader '{0}' could not be found", shaderData.Name);
        }

        if (GetUniformLocation(shaderProgram, "normalMatrix")
            .TryPickProblems(out problems, out var normalLocation))
        {
            return problems.Prepend("Normal location for shader '{0}' could not be found", shaderData.Name);
        }

        Cleanup(shaderProgram, shaderPrograms);


        return new Registration(shaderProgram, worldPositionLocation, normalLocation);
    }

    private static void Cleanup(ShaderProgram shaderProgram, IReadOnlyList<uint> shaders)
    {
        foreach (var shader in shaders)
        {
            GameManager.GL.DetachShader(shaderProgram.Handle, shader);
            GameManager.GL.DeleteShader(shader);
        }
    }

    private static Result<uint> LoadShader(string shaderSource, ShaderType shaderType)
    {
        var vertexShader = GameManager.GL.CreateShader(shaderType);
        GameManager.GL.ShaderSource(vertexShader, shaderSource);
        GameManager.GL.CompileShader(vertexShader);
        GameManager.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);

        if (vStatus != (int)GLEnum.True)
        {
            return new ResultProblem("Shader failed to compile with message '{0}'", GameManager.GL.GetShaderInfoLog(vertexShader));
        }

        return vertexShader;
    }

    private static Result<ShaderProgram> CreateShaderProgram(IReadOnlyList<uint> shaders)
    {
        if (shaders.Count == 0)
        {
            return new ResultProblem("Attempted to create empty shader program");
        }

        var shaderProgram = GameManager.GL.CreateProgram();

        foreach (var shader in shaders)
        {
            GameManager.GL.AttachShader(shaderProgram, shader);
        }

        GameManager.GL.LinkProgram(shaderProgram);
        GameManager.GL.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Shader program failed to link with message: '{0}'",
                GameManager.GL.GetProgramInfoLog(shaderProgram));
        }

        return new ShaderProgram(shaderProgram);
    }

    private static Result<int?> GetUniformLocation(ShaderProgram shaderProgram, string uniformName)
    {
        var worldPositionLocation = GameManager.GL.GetUniformLocation(shaderProgram.Handle, uniformName);
        if (worldPositionLocation == -1)
        {
            return (int?)null;
        }

        return worldPositionLocation;
    }

    public Result Unregister(Registration registration)
    {
        GameManager.GL.DeleteProgram(registration.ShaderProgram.Handle);

        return Result.Success();
    }
}