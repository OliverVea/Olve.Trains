using System.Diagnostics;
using MemoryPack;

namespace Olve.Engine3D.Rendering.Primitives;

[MemoryPackable]
[DebuggerDisplay("({A},{B},{C})")]
public readonly partial record struct TriangleIndex(uint A, uint B, uint C)
{
    public static TriangleIndex operator +(TriangleIndex a, uint d) =>
        new(a.A + d, a.B + d, a.C + d);
}