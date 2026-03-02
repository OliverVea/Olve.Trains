using Olve.Engine3D.Assets.Entities;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Assets.Meshes;

public class MeshLoadingManager(AssetLoader assetLoader, MeshManager meshManager)
{
    private readonly Dictionary<string, Id<Mesh>> _registrations = new();

    public Result<Id<Mesh>> LoadMesh(AssetPath<MeshData> meshPath)
    {
        var pathKey = meshPath.Path.Absolute.Path;
        if (_registrations.TryGetValue(pathKey, out var cachedMeshId))
        {
            return cachedMeshId;
        }

        if (assetLoader
            .LoadAsset(meshPath)
            .TryPickProblems(out var problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh: {0}", meshPath);
        }

        if (meshManager.Register(meshData).TryPickProblems(out problems, out var meshId))
        {
            return problems.Prepend("Failed to register mesh: {0}", meshPath);
        }

        _registrations[pathKey] = meshId;
        return meshId;
    }
}
