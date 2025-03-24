using Olve.Engine3D.Graphics.Entities;

namespace Olve.Trains.AssetPipeline.Assets;

public class MeshAsset
{
    public required string Name { get; set; }
    public required string Source { get; set; }
    public required string Destination { get; set; }
    public required MeshData MeshData { get; set; }
}