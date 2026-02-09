using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Parameters;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLShaderManager(Provider<GL> glProvider, TextureSlotManager textureSlotManager) : IOpenGLEntityManager<ShaderData, OpenGLShaderManager.Registration>
{
    public Result LoadShaderInOpenGL(ShaderProgram shaderProgram, RenderingParameters parameters)
    {
        glProvider.Value.UseProgram(shaderProgram.Handle);
        foreach (var p in parameters.Parameters)
        {
            var r = p.SetUniforms(glProvider.Value, shaderProgram, textureSlotManager);
            if (r.Failed) return r;
        }
        return Result.Success();
    }

    /// <summary>
    /// Applies additional rendering parameters to an already-bound shader.
    /// Used for per-entity parameter overrides.
    /// </summary>
    public Result ApplyParameters(ShaderProgram shaderProgram, RenderingParameters parameters)
    {
        foreach (var p in parameters.Parameters)
        {
            var r = p.SetUniforms(glProvider.Value, shaderProgram, textureSlotManager);
            if (r.Failed) return r;
        }
        return Result.Success();
    }

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

    private void Cleanup(ShaderProgram shaderProgram, IReadOnlyList<uint> shaders)
    {
        foreach (var shader in shaders)
        {
            glProvider.Value.DetachShader(shaderProgram.Handle, shader);
            glProvider.Value.DeleteShader(shader);
        }
    }

    private Result<uint> LoadShader(string shaderSource, ShaderType shaderType)
    {
        var vertexShader = glProvider.Value.CreateShader(shaderType);
        glProvider.Value.ShaderSource(vertexShader, shaderSource);
        glProvider.Value.CompileShader(vertexShader);
        glProvider.Value.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);

        if (vStatus != (int)GLEnum.True)
        {
            return new ResultProblem("Shader failed to compile with message '{0}'", glProvider.Value.GetShaderInfoLog(vertexShader));
        }

        return vertexShader;
    }

    private Result<ShaderProgram> CreateShaderProgram(IReadOnlyList<uint> shaders)
    {
        if (shaders.Count == 0)
        {
            return new ResultProblem("Attempted to create empty shader program");
        }

        var shaderProgram = glProvider.Value.CreateProgram();

        foreach (var shader in shaders)
        {
            glProvider.Value.AttachShader(shaderProgram, shader);
        }

        glProvider.Value.LinkProgram(shaderProgram);
        glProvider.Value.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out var lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            return new ResultProblem(
                "Shader program failed to link with message: '{0}'",
                glProvider.Value.GetProgramInfoLog(shaderProgram));
        }

        return new ShaderProgram(shaderProgram);
    }

    private Result<int?> GetUniformLocation(ShaderProgram shaderProgram, string uniformName)
    {
        var worldPositionLocation = glProvider.Value.GetUniformLocation(shaderProgram.Handle, uniformName);
        if (worldPositionLocation == -1)
        {
            return (int?)null;
        }

        return worldPositionLocation;
    }

    public Result Unregister(Registration registration)
    {
        glProvider.Value.DeleteProgram(registration.ShaderProgram.Handle);

        return Result.Success();
    }
}