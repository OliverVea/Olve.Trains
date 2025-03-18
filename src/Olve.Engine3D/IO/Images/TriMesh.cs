namespace Olve.Engine3D.IO.Images;

public class TriMesh
{
    public required Vector3D<float>[] Vertices { get; set; }
    public required int[] Indices { get; set; }
}