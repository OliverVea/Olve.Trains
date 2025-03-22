using MemoryPack;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Graphics;
using Olve.Operations;
using Olve.Results;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using File = System.IO.File;

namespace Olve.Engine3D.AssetPipeline.Operations;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger) : IAsyncOperation<ProcessAssets.Request, ProcessAssets.Response>
{
    private const string ModelSourceDirectory = "/app/temp/assets/models";
    private const string MeshOutputDirectory = "/app/output/meshes";
    
    public record Request(IReadOnlyList<FileInfo> AssetFiles);

    public record Response;

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing assets");
        
        if (!Directory.Exists(ModelSourceDirectory))
        {
            return new ResultProblem("Model source directory '{0}' does not exist", ModelSourceDirectory);
        }
        
        Directory.CreateDirectory(MeshOutputDirectory);
        
        var meshesResult = LoadMeshes();
        if (meshesResult.TryPickProblems(out var problems, out var meshAssets))
        {
            return problems.Prepend("Failed to load meshes");
        }

        foreach (var meshAsset in meshAssets)
        {
            using var assetStream = new MemoryStream();
        
            await MemoryPackSerializer.SerializeAsync(assetStream, meshAsset.Mesh, cancellationToken: ct);
            
            var assetOutputPath = Path.Combine(MeshOutputDirectory, $"{meshAsset.Name}.mesh");
            
            ReadOnlyMemory<byte> assetBytes = assetStream.GetBuffer();
            
            await File.WriteAllBytesAsync(assetOutputPath, assetBytes, ct);
            
            logger.LogDebug("Wrote asset '{0}' to '{1} with '{2}' bytes of data.", meshAsset.Name, assetOutputPath, assetBytes.Length);
        }
        
        return new Response();
    }

    private unsafe Result<List<MeshAsset>> LoadMeshes()
    {
        var modelFiles = Directory.GetFiles(ModelSourceDirectory, "*.fbx", SearchOption.AllDirectories);
        
        logger.LogInformation("Processing {ModelCount} models", modelFiles.Length);

        var assimp = Assimp.GetApi();
        List<MeshAsset> meshes = [];

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

            var vertices = new Vector3D<float>[meshPtr->MNumVertices];
            var indices = new TriangleIndex[meshPtr->MNumFaces];
            var normals = new Vector3D<float>[meshPtr->MNumVertices];

            for (var i = 0; i < meshPtr->MNumVertices; i++)
            {
                var vertex = meshPtr->MVertices[i];
                vertices[i] = new Vector3D<float>(vertex.X, vertex.Y, vertex.Z);

                var normal = meshPtr->MNormals[i];
                normals[i] = new Vector3D<float>(normal.X, normal.Y, normal.Z);
            }

            for (var i = 0; i < meshPtr->MNumFaces; i++)
            {
                var face = meshPtr->MFaces[i];
                indices[i] = new TriangleIndex(face.MIndices[0], face.MIndices[1], face.MIndices[2]);
            }

            Graphics.Mesh mesh = new()
            {
                Vertices = vertices,
                Indices = indices,
                Normals = normals,
            };
            
            var assetName = Path.GetFileNameWithoutExtension(modelFile);
            var assetSourcePath = Path.GetFullPath(modelFile);

            MeshAsset asset = new()
            {
                Name = assetName,
                SourcePath = assetSourcePath,
                Mesh = mesh
            };

            meshes.Add(asset);
        }

        logger.LogDebug("Loaded {ModelCount} meshes", modelFiles.Length);
        
        return meshes;
    }
}

public class MeshAsset
{
    public required string Name { get; set; }
    public required string SourcePath { get; set; }
    public required Graphics.Mesh Mesh { get; set; }
}