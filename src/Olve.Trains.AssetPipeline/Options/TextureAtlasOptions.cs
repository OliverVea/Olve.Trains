namespace Olve.Trains.AssetPipeline.Options;

public class TextureAtlasOptions : IAssetOptions
{
    public string SectionName => "TextureAtlas";
    public string NamespacePostfix { get; set; } = ".TextureAtlases";
    public string OutputFolder { get; set; } = "TextureAtlases";
}
