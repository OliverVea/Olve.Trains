using MemoryPack;

namespace Olve.Engine3D.Graphics;

[MemoryPackable]
public readonly partial record struct TriangleIndex(uint A, uint B, uint C)
{
    public static TriangleIndex operator +(TriangleIndex a, uint d) =>
        new(a.A + d, a.B + d, a.C + d);
}