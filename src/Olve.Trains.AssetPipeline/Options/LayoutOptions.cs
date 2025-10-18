namespace Olve.Trains.AssetPipeline.Options;

public class LayoutOptions : IAssetOptions
{
    public string SectionName => "Layout";

    /// <summary>
    /// Directory containing layout XML files. Required when layouts target is enabled.
    /// </summary>
    public string? LayoutsDirectory { get; set; }

    /// <summary>
    /// Namespace to use for generated layout classes.
    /// </summary>
    public string NamespacePostfix { get; set; } = ".Layouts";

    public string OutputFolder { get; set; } = "Layouts";
}