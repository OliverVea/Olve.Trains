namespace Olve.Trains.AssetPipeline.Options;

public class FontOptions : IAssetOptions
{
    public string SectionName => "Font";

    /// <summary>
    /// Namespace to use for generated font classes.
    /// </summary>
    public string NamespacePostfix { get; set; } = ".Fonts";

    public string OutputFolder { get; set; } = "Fonts";

    /// <summary>
    /// Atlas dimensions (width, height). Default is 512x512.
    /// </summary>
    public int AtlasWidth { get; set; } = 512;
    public int AtlasHeight { get; set; } = 512;

    /// <summary>
    /// Glyph size in pixels. Default is 64.
    /// </summary>
    public int GlyphSize { get; set; } = 64;

    /// <summary>
    /// Pixel range for MSDF. Default is 4.
    /// </summary>
    public int PixelRange { get; set; } = 4;
}