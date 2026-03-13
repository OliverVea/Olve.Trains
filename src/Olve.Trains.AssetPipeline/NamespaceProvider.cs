using Microsoft.Extensions.Options;
using Olve.Trains.AssetPipeline.Options;

namespace Olve.Trains.AssetPipeline;

public class NamespaceProvider(IOptions<BuildOptions> buildOptions, IOptions<ShaderOptions> shaderOptions, IOptions<TerrainOptions> terrainOptions, IOptions<MeshOptions> meshOptions, IOptions<TextureOptions> textureOptions, IOptions<LayoutOptions> layoutOptions, IOptions<FontOptions> fontOptions, IOptions<TextureAtlasOptions> textureAtlasOptions)
{
    public string LayoutNamespace => buildOptions.Value.BaseNamespace + layoutOptions.Value.NamespacePostfix;
    public string ShaderNamespace => buildOptions.Value.BaseNamespace + shaderOptions.Value.NamespacePostfix;
    public string MeshNamespace => buildOptions.Value.BaseNamespace + meshOptions.Value.NamespacePostfix;
    public string TextureNamespace => buildOptions.Value.BaseNamespace + textureOptions.Value.NamespacePostfix;
    public string TerrainNamespace => buildOptions.Value.BaseNamespace + terrainOptions.Value.NamespacePostfix;
    public string FontNamespace => buildOptions.Value.BaseNamespace + fontOptions.Value.NamespacePostfix;
    public string TextureAtlasNamespace => buildOptions.Value.BaseNamespace + textureAtlasOptions.Value.NamespacePostfix;
}