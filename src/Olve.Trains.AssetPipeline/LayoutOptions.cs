namespace Olve.Trains.AssetPipeline;

public class LayoutOptions
{
    /// <summary>
    /// Directory containing layout XML files. Defaults to Paths.LayoutsSourceFolder.
    /// </summary>
    public string LayoutsDirectory { get; set; } = Paths.LayoutsSourceFolder;

    /// <summary>
    /// Namespace to use for generated layout classes.
    /// </summary>
    public string Namespace { get; set; } = "Olve.Trains.resources.layouts";
}
