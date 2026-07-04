namespace Olve.Trains.AssetPipeline.Options;

/// <summary>
/// Configuration for the source art the pipeline compiles.
/// </summary>
public class AssetOptions : IAssetOptions
{
    public string SectionName => "Asset";

    /// <summary>
    /// Directory containing the committed source art (fbx/png/tga/ora/ttf, tracked via git LFS).
    /// Required when any asset target (meshes/textures/terrains/fonts/texture atlases) is enabled.
    /// </summary>
    public string? SourceDirectory { get; set; }
}
