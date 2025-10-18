namespace Olve.Trains.AssetPipeline.Options;

public class TerrainOptions : IAssetOptions
{
    public string SectionName => "Terrain";

    public string OutputFolder { get; set; } = "Terrains";
    public string NamespacePostfix { get; set; } = ".Terrains";

    public float HeightPerStep { get ; set; } = 0.25f;
    public int ZeroHeight { get ; set; } = 128;
    public int HeightStep { get ; set; } = 8;
}