using Microsoft.Extensions.Options;

namespace Olve.Trains.AssetPipeline.Options;

public class BuildOptions : IAssetOptions
{
    public string SectionName => "Build";

    public IReadOnlyList<string> Targets { get; set; } = [];
    public string BuildDirectory { get; set; } = Directory.CreateTempSubdirectory().FullName;
    public string? OutputDirectory { get; set; }
    public string BaseNamespace { get; set; } = "Generated";
}