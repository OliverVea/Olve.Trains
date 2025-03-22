using MemoryPack;

namespace Olve.Engine3D.Graphics;

[MemoryPackable]
public partial class Mesh
{
    public required Vector3D<float>[] Vertices { get; set; }
    public required Vector3D<float>[] Normals { get; set; }
    public required TriangleIndex[] Indices { get; set; }
}