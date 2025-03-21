namespace Olve.Engine3D.Graphics;

public static class GeometryDataMapper
{
    public static GeometryData<LineIndex> ToWireframe(this GeometryData<TriangleIndex> triangleGeometry)
    {
        var lineSet = new HashSet<LineIndex>();

        foreach (var triangle in triangleGeometry.Indices)
        {
            var edges = new[]
            {
                new LineIndex(triangle.A, triangle.B),
                new LineIndex(triangle.B, triangle.C),
                new LineIndex(triangle.C, triangle.A)
            };

            foreach (var edge in edges)
            {
                var orderedEdge = edge.A < edge.B ? edge : new LineIndex(edge.B, edge.A);
                lineSet.Add(orderedEdge);
            }
        }

        return new GeometryData<LineIndex>
        {
            Vertices = triangleGeometry.Vertices,
            Indices = lineSet.ToArray()
        };
    }
}