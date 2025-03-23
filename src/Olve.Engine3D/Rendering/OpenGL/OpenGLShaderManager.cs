using Olve.Engine3D.Graphics.Shaders;
using Olve.Engine3D.Rendering.OpenGL.Types;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLShaderManager : IOpenGLEntityManager<ShaderData, ShaderProgram>
{
    public Result<ShaderProgram> Register(ShaderData shaderData)
    {
        // Load shader
        var vertexShader = GameManager.GL.CreateShader(ShaderType.VertexShader);
        GameManager.GL.ShaderSource(vertexShader, shaderData.VertexSource);
        GameManager.GL.CompileShader(vertexShader);
        GameManager.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);

        if (vStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Vertex shader for '{0}' failed to compile with message '{1}'",
                shaderData.Name,
                GameManager.GL.GetShaderInfoLog(vertexShader));
        }

        var fragmentShader = GameManager.GL.CreateShader(ShaderType.FragmentShader);
        GameManager.GL.ShaderSource(fragmentShader, shaderData.FragmentSource);
        GameManager.GL.CompileShader(fragmentShader);
        GameManager.GL.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var fStatus);

        if (fStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Fragment shader for '{0}' failed to compile with message '{1}'",
                shaderData.Name,
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

        return shaderProgram;
    }

    public Result Unregister(ShaderProgram registration)
    {
        GameManager.GL.DeleteProgram(registration.Handle);

        return Result.Success();
    }
}