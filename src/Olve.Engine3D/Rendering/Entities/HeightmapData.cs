using MemoryPack;

namespace Olve.Engine3D.Rendering.Entities;

[MemoryPackable]
public partial class HeightmapData
{
    public required float[] Heights { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
}