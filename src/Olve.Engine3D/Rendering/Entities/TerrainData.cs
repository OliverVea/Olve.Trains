using MemoryPack;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class TerrainData
{
    public Id<TerrainData> Id { get; init; } = Olve.Utilities.Ids.Id.New<TerrainData>();
    public required HeightmapData Heightmap { get; set; }
}