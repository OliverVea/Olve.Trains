namespace Olve.Trains.AssetPipeline.Options;

public class MeshOptions : IAssetOptions
{
    public string SectionName => "Mesh";

    public string OutputFolder { get; set; } =  "Meshes";
    public string NamespacePostfix { get; set; } = ".Meshes";
}