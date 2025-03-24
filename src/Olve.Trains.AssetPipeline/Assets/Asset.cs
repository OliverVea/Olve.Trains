namespace Olve.Trains.AssetPipeline.Assets;

public class Asset<T>
{
    public required string Name { get; set; }
    public required string Source { get; set; }
    public required string Destination { get; set; }
    public required T Data { get; set; }
}