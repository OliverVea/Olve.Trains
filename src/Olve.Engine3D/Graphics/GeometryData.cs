namespace Olve.Engine3D.Graphics;

public class GeometryData<TIndex>
    where TIndex : unmanaged
{
    public required Vector3D<float>[] Vertices { get; set; }
    public required TIndex[] Indices { get; set; }
}