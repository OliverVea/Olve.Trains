using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;

namespace Olve.Trains.Scenes.GameRendering;

public static class MeshDataMarshalHelper
{
    public static (float[] VertexFloats, uint[] Indices) Marshal<TVertex>(MeshData meshData)
        where TVertex : struct, IVertexData, IWithPosition3D<TVertex>, IWithNormal3D<TVertex>, IWithTexCoords2D<TVertex>
    {
        var vertices = new TVertex[meshData.VertexCount];
        meshData.Populate(vertices);

        var vertexFloats = new float[meshData.VertexCount * TVertex.FloatCount];
        var span = vertexFloats.AsSpan();
        var offset = 0;
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            vertices[i].WriteTo(span.Slice(offset, TVertex.FloatCount));
            offset += TVertex.FloatCount;
        }

        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        return (vertexFloats, indices);
    }
}
