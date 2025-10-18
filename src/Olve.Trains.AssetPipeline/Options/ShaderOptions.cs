namespace Olve.Trains.AssetPipeline.Options;

public class ShaderOptions : IAssetOptions
{
    public string SectionName => "Shader";

    /// <summary>
    /// Directory containing shader source files. Required when shaders target is enabled.
    /// </summary>
    public string? ShadersDirectory { get; set; }

    public string OutputFolder { get; set; } = "Shaders";
    public string NamespacePostfix { get; set; } = ".Shaders";
}
