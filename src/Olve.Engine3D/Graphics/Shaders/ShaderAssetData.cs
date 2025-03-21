using Olve.Engine3D.Assets;

namespace Olve.Engine3D.Graphics.Shaders;

public class ShaderAssetData
{
    public AssetPath<VertexShaderAsset> VertexShaderPath { get; init; }
    public AssetPath<FragmentShaderAsset> FragmentShaderPath { get; init; }

    public required ShaderParameterNames ShaderParameterNames { get; init; }
}