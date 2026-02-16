namespace Olve.Trains.AssetPipeline.Options;

/// <summary>
/// Configuration for S3 asset retrieval.
/// </summary>
public class S3Options : IAssetOptions
{
    public string SectionName => "S3";

    /// <summary>
    /// S3 bucket name.
    /// </summary>
    public string? Bucket { get; set; }

    /// <summary>
    /// S3 bucket prefix for asset files.
    /// </summary>
    public string Prefix { get; set; } = "/";

    /// <summary>
    /// S3 access key.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// S3 secret key.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Initial timeout in milliseconds when listing objects.
    /// </summary>
    public int TimeoutMs { get; set; } = 20000;

    /// <summary>
    /// If true, pipeline continues even if S3 operations fail.
    /// </summary>
    public bool AllowFailure { get; set; } = false;

    /// <summary>
    /// If true, skips S3 download and loads assets directly from the build directory.
    /// Useful for local development without S3 credentials.
    /// </summary>
    public bool UseLocalAssets { get; set; } = false;
}
