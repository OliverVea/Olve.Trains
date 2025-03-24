using Olve.Engine3D.Graphics.Shaders;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLShaderManager : IOpenGLEntityManager<ShaderData, ShaderProgram>
{
    public Result<ShaderProgram> Register(ShaderData shaderData)
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

        if (CreateShaderProgram(vertexShader, fragmentShader)
            .TryPickProblems(out problems, out var shaderProgram))
        {
            return problems.Prepend("Shader program '{0}' could not be created", shaderData.Name);
        }

        Cleanup(shaderProgram, vertexShader, fragmentShader);

        return shaderProgram;
    }

    private static void Cleanup(ShaderProgram shaderProgram, uint vertexShader, uint fragmentShader)
    {
        GameManager.GL.DetachShader(shaderProgram.Handle, vertexShader);
        GameManager.GL.DetachShader(shaderProgram.Handle, fragmentShader);
        GameManager.GL.DeleteShader(vertexShader);
        GameManager.GL.DeleteShader(fragmentShader);
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

    private static Result<ShaderProgram> CreateShaderProgram(params ReadOnlySpan<uint> shaders)
    {
        if (shaders.Length == 0)
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

    public Result Unregister(ShaderProgram registration)
    {
        GameManager.GL.DeleteProgram(registration.Handle);

        return Result.Success();
    }
}