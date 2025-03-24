using Microsoft.Extensions.Logging;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Results;
using Silk.NET.Assimp;
using Silk.NET.Maths;

namespace Olve.Trains.AssetPipeline.Assets;

public class MeshFileReader(ILogger<MeshFileReader> logger)
{
    public unsafe Result<IReadOnlyList<Asset<MeshData>>> LoadMeshes(IReadOnlyList<FileInfo> files)
    {
        var modelFiles = files
            .Where(f => f.Extension is ".fbx" or ".obj")
            .Select(f => f.FullName)
            .ToArray();

        if (modelFiles.Length == 0)
        {
            logger.LogWarning("No model files found");
            return Array.Empty<Asset<MeshData>>();
        }

        logger.LogInformation("Processing {ModelCount} models", modelFiles.Length);

        var assimp = Assimp.GetApi();
        List<Asset<MeshData>> meshes = [];

        foreach (var modelFile in modelFiles)
        {
            var scene = assimp.ImportFile(modelFile, (uint)PostProcessSteps.Triangulate);

            if (scene == null)
            {
                return new ResultProblem("Failed to load model '{0}'", modelFile);
            }

            var meshCount = scene->MNumMeshes;
            if (meshCount != 1)
            {
                return new ResultProblem("Expected 1 mesh in model '{0}', but found {1}", modelFile, meshCount);
            }

            logger.LogDebug("Processing {ModelCount} model", modelFiles.Length);

            var meshPtr = scene->MMeshes[0];

            var positions = new Vector3D<float>[meshPtr->MNumVertices];
            var indices = new TriangleIndex[meshPtr->MNumFaces];
            var normals = new Vector3D<float>[meshPtr->MNumVertices];
            var textureCoords = new Vector2D<float>[meshPtr->MNumVertices];

            for (var i = 0; i < meshPtr->MNumVertices; i++)
            {
                var vertex = meshPtr->MVertices[i];
                positions[i] = new Vector3D<float>(vertex.X, vertex.Y, vertex.Z);

                var normal = meshPtr->MNormals[i];
                normals[i] = new Vector3D<float>(normal.X, normal.Y, normal.Z);
            }

            for (var i = 0; i < meshPtr->MNumFaces; i++)
            {
                var face = meshPtr->MFaces[i];
                indices[i] = new TriangleIndex(face.MIndices[0], face.MIndices[1], face.MIndices[2]);
            }

            for (var i = 0; i < meshPtr->MNumVertices; i++)
            {
                var textureCoord = meshPtr->MTextureCoords[0][i];
                textureCoords[i] = new Vector2D<float>(textureCoord.X, textureCoord.Y);
            }

            MeshData meshData = new()
            {
                Positions = positions,
                Indices = indices,
                Normals = normals,
                TextureCoordinates = textureCoords
            };

            var assetName = Path.GetFileNameWithoutExtension(modelFile);
            var assetSource = Path.GetFullPath(modelFile);
            var assetDestination = "meshes/" + assetName + ".mesh";

            Asset<MeshData> asset = new()
            {
                Name = assetName,
                Source = assetSource,
                Destination = assetDestination,
                Data = meshData,
            };

            meshes.Add(asset);
        }

        logger.LogDebug("Loaded {ModelCount} meshes", modelFiles.Length);

        return meshes;
    }
}