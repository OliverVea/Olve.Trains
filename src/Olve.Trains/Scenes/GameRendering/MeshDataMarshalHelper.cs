eusing Olve.Engine3D.Assets.Entities;
using Olve.Generated.Shaders;

namespace Olve.Trains.Scenes.GameRendering;

public static class MeshDataMarshalHelper
{
    public static (float[] VertexFloats, uint[] Indices) MarshalDefaultShader(MeshData meshData)
    {
        var vertexFloats = new float[meshData.VertexCount * Shaders.Default.Vertex.FloatCount];
        var span = vertexFloats.AsSpan();
        var offset = 0;
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            var vertex = new Shaders.Default.Vertex(
                meshData.Positions[i],
                meshData.Normals[i],
                meshData.TextureCoordinates[i]);
            vertex.WriteTo(span.Slice(offset, Shaders.Default.Vertex.FloatCount));
            offset += Shaders.Default.Vertex.FloatCount;
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
