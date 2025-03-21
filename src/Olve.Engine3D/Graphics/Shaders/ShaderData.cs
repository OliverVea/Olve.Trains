using Olve.Engine3D.Assets;

namespace Olve.Engine3D.Graphics.Shaders;

public class ShaderData
{
    public ShaderSource<VertexShaderAsset> VertexShaderSource { get; init; }
    public ShaderSource<FragmentShaderAsset> FragmentShaderSource { get; init; }

    public required ShaderParameterNames ShaderParameterNames { get; init; }
}