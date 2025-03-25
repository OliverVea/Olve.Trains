using MemoryPack;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class TerrainData
{
    public required HeightmapData Heightmap { get; set; }
}