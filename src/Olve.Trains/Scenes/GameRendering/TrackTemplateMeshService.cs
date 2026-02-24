using Olve.Engine3D.Rendering;

namespace Olve.Trains.Scenes.GameRendering;

public static class TrackTemplateMeshService
{
    private const int Subdivisions = 64;
    private const float HalfGauge = 0.07f;
    private const float RailHalfWidth = 0.005f;
    private const float RailHeight = 0.01f;

    public static (TVertex[] Vertices, uint[] Indices) Generate<TVertex>()
        where TVertex : struct, IWithPosition3D<TVertex>, IWithNormal3D<TVertex>
    {
        var ringCount = Subdivisions + 1;
        var vertsPerRing = 16; // 8 per rail × 2 rails
        var vertexCount = ringCount * vertsPerRing;
        var vertices = new TVertex[vertexCount];

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
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx - RailHalfWidth, 0, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(-1, 0, 0));
                vi++;
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx - RailHalfWidth, RailHeight, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(-1, 0, 0));
                vi++;

                // Face 1: right (+x normal)
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx + RailHalfWidth, 0, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(1, 0, 0));
                vi++;
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx + RailHalfWidth, RailHeight, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(1, 0, 0));
                vi++;

                // Face 2: top (+y normal)
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx - RailHalfWidth, RailHeight, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(0, 1, 0));
                vi++;
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx + RailHalfWidth, RailHeight, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(0, 1, 0));
                vi++;

                // Face 3: bottom (-y normal)
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx - RailHalfWidth, 0, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(0, -1, 0));
                vi++;
                vertices[vi] = TVertex.WithPosition(vertices[vi], new(cx + RailHalfWidth, 0, t));
                vertices[vi] = TVertex.WithNormal(vertices[vi], new(0, -1, 0));
                vi++;
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
