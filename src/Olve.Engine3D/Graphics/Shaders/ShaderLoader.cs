using Olve.Engine3D.Assets;
using Silk.NET.OpenGL;
using static Olve.Engine3D.GameManager;

namespace Olve.Engine3D.Graphics.Shaders;

public static class ShaderLoader
{
    public static Result<Shader> Load(AssetPath<VertexShaderAsset> vertexPath, AssetPath<FragmentShaderAsset> fragmentPath)
    {
        var vertexSource = vertexPath.ReadShader();
        if (vertexSource.TryPickProblems(out var problems, out var vertexShader))
        {
            return problems.Prepend("Failed to load vertex shader");
        }

        var fragmentSource = fragmentPath.ReadShader();
        if (fragmentSource.TryPickProblems(out problems, out var fragmentShader))
        {
            return problems.Prepend("Failed to load fragment shader");
        }

        return Create(vertexShader, fragmentShader);
    }

    public static Result<Shader> Create(ShaderSource<VertexShaderAsset> vertexSource, ShaderSource<FragmentShaderAsset> fragmentSource)
    {
        var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, vertexSource.SourceCode);
        Gl.CompileShader(vertexShader);
        Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Vertex shader '{0}' failed to compile with message '{1}'",
                vertexSource.Path,
                Gl.GetShaderInfoLog(vertexShader));
        }

        var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        Gl.ShaderSource(fragmentShader, fragmentSource.SourceCode);
        Gl.CompileShader(fragmentShader);
        Gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var fStatus);
        if (fStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Fragment shader '{0}' failed to compile with message '{1}'",
                fragmentSource.Path,
                Gl.GetShaderInfoLog(fragmentShader));
        }

        var shaderProgram = Gl.CreateProgram();

        Gl.AttachShader(shaderProgram, vertexShader);
        Gl.AttachShader(shaderProgram, fragmentShader);

        Gl.LinkProgram(shaderProgram);

        Gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Shader program failed to link with message: '{0}'",
                Gl.GetProgramInfoLog(shaderProgram));
        }

        Gl.DetachShader(shaderProgram, vertexShader);
        Gl.DetachShader(shaderProgram, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        return new Shader(new ShaderProgram(shaderProgram));
    }
}
