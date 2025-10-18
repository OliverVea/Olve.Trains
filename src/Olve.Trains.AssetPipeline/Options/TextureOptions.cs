namespace Olve.Trains.AssetPipeline.Options;

public class TextureOptions : IAssetOptions
{
    public string SectionName => "Texture";

    public string OutputFolder { get; set; } = "Textures";
    public string NamespacePostfix { get; set; } = ".Textures";
}