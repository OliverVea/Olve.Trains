using MemoryPack;
using StrictId;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class TerrainData
{
    public Id<TerrainData> Id { get; init; } = Id<TerrainData>.NewId();
    public required HeightmapData Heightmap { get; set; }
}