using Olve.Engine3D.Assets;
using Olve.Engine3D.Graphics.OpenGL;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Graphics.Shaders;

public static class ShaderLoader
{
    public static Result<ShaderData> Load(ShaderAssetData shaderAssetData)
    {
        var vertexSource = shaderAssetData.VertexShaderPath.ReadShader();
        if (vertexSource.TryPickProblems(out var problems, out var vertexShader))
        {
            return problems.Prepend("Failed to load vertex shader");
        }

        var fragmentSource = shaderAssetData.FragmentShaderPath.ReadShader();
        if (fragmentSource.TryPickProblems(out problems, out var fragmentShader))
        {
            return problems.Prepend("Failed to load fragment shader");
        }

        return new ShaderData
        {
            FragmentShaderSource = fragmentShader,
            VertexShaderSource = vertexShader,
            ShaderParameterNames = shaderAssetData.ShaderParameterNames
        };
    }
}
