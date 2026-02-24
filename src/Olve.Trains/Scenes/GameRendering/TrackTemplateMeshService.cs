using Olve.Engine3D.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public static class TrackTemplateMeshService
{
    public readonly record struct TrackVertex(
        Vector3D<float> Position,
        Vector3D<float> Normal) : IVertexData
    {
        public static int FloatCount => 6;

        public void WriteTo(Span<float> buffer)
        {
            var i = 0;
            buffer[i++] = Position.X; buffer[i++] = Position.Y; buffer[i++] = Position.Z;
            buffer[i++] = Normal.X;   buffer[i++] = Normal.Y;   buffer[i++] = Normal.Z;
        }

        public static void ConfigureAttributes(GL gl)
        {
            const uint stride = 6 * sizeof(float);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (nint)0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (nint)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
        }
    }

    private const int Subdivisions = 64;
    private const float HalfGauge = 0.07f;
    private const float RailHalfWidth = 0.005f;
    private const float RailHeight = 0.01f;

    public static (TrackVertex[] Vertices, uint[] Indices) Generate()
    {
        // Each rail is a box with 4 side faces extruded along Z.
        // Two rails: left at x = -HalfGauge, right at x = +HalfGauge.
        // Each box cross-section has 4 corners, but for proper normals we need
        // separate vertices per face. 4 faces × 2 verts per ring = 8 verts per ring per rail.
        // Two rails = 16 verts per ring.
        // (Subdivisions + 1) rings.

        var ringCount = Subdivisions + 1;
        var vertsPerRing = 16; // 8 per rail × 2 rails
        var vertexCount = ringCount * vertsPerRing;
        var vertices = new TrackVertex[vertexCount];

        // 4 quads per rail × 2 tris per quad × 2 rails × Subdivisions segments
        var indexCount = 4 * 2 * 2 * Subdivisions * 3;
        var indices = new uint[indexCount];

        float[] railCenters = [-HalfGauge, HalfGauge];

        var vi = 0;
        for (var ring = 0; ring < ringCount; ring++)
        {
            var t = (float)ring / Subdivisions;

            foreach (var cx in railCenters)
            {
                // 8 vertices per rail per ring (2 per face for 4 faces)
                // Face 0: left  (-x normal)
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx - RailHalfWidth, 0, t),
                    new Vector3D<float>(-1, 0, 0));
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx - RailHalfWidth, RailHeight, t),
                    new Vector3D<float>(-1, 0, 0));

                // Face 1: right (+x normal)
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx + RailHalfWidth, 0, t),
                    new Vector3D<float>(1, 0, 0));
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx + RailHalfWidth, RailHeight, t),
                    new Vector3D<float>(1, 0, 0));

                // Face 2: top (+y normal)
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx - RailHalfWidth, RailHeight, t),
                    new Vector3D<float>(0, 1, 0));
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx + RailHalfWidth, RailHeight, t),
                    new Vector3D<float>(0, 1, 0));

                // Face 3: bottom (-y normal)
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx - RailHalfWidth, 0, t),
                    new Vector3D<float>(0, -1, 0));
                vertices[vi++] = new TrackVertex(
                    new Vector3D<float>(cx + RailHalfWidth, 0, t),
                    new Vector3D<float>(0, -1, 0));
            }
        }

        // Generate indices: connect ring[i] to ring[i+1] for each face
        var ii = 0;
        for (var seg = 0; seg < Subdivisions; seg++)
        {
            var ringBase0 = (uint)(seg * vertsPerRing);
            var ringBase1 = (uint)((seg + 1) * vertsPerRing);

            // For each rail (0 and 1), for each face (0..3)
            for (var rail = 0; rail < 2; rail++)
            {
                var railOffset = (uint)(rail * 8);

                for (var face = 0; face < 4; face++)
                {
                    var faceOffset = (uint)(face * 2);
                    var v0 = ringBase0 + railOffset + faceOffset;     // bottom-left
                    var v1 = ringBase0 + railOffset + faceOffset + 1; // top-left
                    var v2 = ringBase1 + railOffset + faceOffset;     // bottom-right
                    var v3 = ringBase1 + railOffset + faceOffset + 1; // top-right

                    // Triangle 1
                    indices[ii++] = v0;
                    indices[ii++] = v2;
                    indices[ii++] = v3;

                    // Triangle 2
                    indices[ii++] = v0;
                    indices[ii++] = v3;
                    indices[ii++] = v1;
                }
            }
        }

        return (vertices, indices);
    }
}
