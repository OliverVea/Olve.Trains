using MemoryPack;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;
using Olve.Operations;
using Olve.Results;
using Scriban;
using Scriban.Runtime;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using File = System.IO.File;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///     Processes game assets and spits them out in /app/output.
/// </summary>
public class ProcessAssets(ILogger<ProcessAssets> logger) : IAsyncOperation<ProcessAssets.Request, ProcessAssets.Response>
{
    private static readonly string TemplateFilePath = Path.Combine(Paths.TemplatesSourceFolder, "ModelsClass.scriban");

    public record Request(IReadOnlyList<FileInfo> AssetFiles);
    public record Response(IReadOnlyList<MeshAsset> MeshAssets);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing assets");
        
        Directory.CreateDirectory(Paths.MeshOutputFolder);
        
        var meshesResult = LoadMeshes(request.AssetFiles);
        if (meshesResult.TryPickProblems(out var problems, out var meshAssets))
        {
            return problems.Prepend("Failed to load meshes");
        }

        foreach (var meshAsset in meshAssets)
        {
            using var assetStream = new MemoryStream();
        
            await MemoryPackSerializer.SerializeAsync(assetStream, meshAsset.MeshData, cancellationToken: ct);
            
            var assetOutputPath = Path.Combine(Paths.MeshOutputFolder, meshAsset.Destination);
            
            ReadOnlyMemory<byte> assetBytes = assetStream.GetBuffer();
            
            await File.WriteAllBytesAsync(assetOutputPath, assetBytes, ct);
            
            logger.LogDebug("Wrote asset '{MeshAssetName}' to '{MeshOutputPath} with '{SizeInBytes}' bytes of data.", meshAsset.Name, assetOutputPath, assetBytes.Length);
        }


        var templateFile = await File.ReadAllTextAsync(TemplateFilePath, ct);

        var template = Template.Parse(templateFile);
        var scriptObject = MapToScriptObject(meshAssets);

        var sourceCode = await template.RenderAsync(scriptObject);
        if (sourceCode is null)
        {
            return new ResultProblem("Failed to write source generated mesh file");
        }

        var outputPath = Path.Combine(Paths.MeshOutputFolder, "Models.cs");

        await File.WriteAllTextAsync(outputPath, sourceCode, ct);

        logger.LogInformation("Processed {Count} model(s) successfully!", meshAssets.Count);
        
        return new Response(meshAssets);
    }

    private unsafe Result<List<MeshAsset>> LoadMeshes(IReadOnlyList<FileInfo> requestAssetFiles)
    {
        var modelFiles = requestAssetFiles
            .Where(f => f.Extension is ".fbx" or ".obj")
            .Select(f => f.FullName)
            .ToArray();
        
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
            var assetDestination = assetName + ".mesh";

            MeshAsset asset = new()
            {
                Name = assetName,
                Source = assetSource,
                MeshData = meshData,
                Destination = assetDestination,
            };

            meshes.Add(asset);
        }

        logger.LogDebug("Loaded {ModelCount} meshes", modelFiles.Length);
        
        return meshes;
    }

    private static ScriptObject MapToScriptObject(IEnumerable<MeshAsset> meshAssets)
    {
        ScriptObject scriptObject = new();

        List<ScriptObject> meshAssetScriptObjects = [];

        foreach (var meshAsset in meshAssets)
        {
            ScriptObject meshAssetScriptObject = new();

            meshAssetScriptObject.Add("Name", meshAsset.Name);
            meshAssetScriptObject.Add("Source", meshAsset.Source);
            meshAssetScriptObject.Add("Destination", meshAsset.Destination);

            meshAssetScriptObjects.Add(meshAssetScriptObject);
        }

        scriptObject.Add("MeshAssets", meshAssetScriptObjects);

        return scriptObject;
    }
}