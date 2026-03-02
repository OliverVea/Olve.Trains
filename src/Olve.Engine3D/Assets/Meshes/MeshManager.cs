using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Assets.Meshes;

public class MeshManager
{
    private readonly Dictionary<Id<Mesh>, (MeshData Data, AABB LocalAABB)> _meshes = new();

    public Event<Id<Mesh>> OnAdded { get; } = new();
    public Event<Id<Mesh>> OnRemoved { get; } = new();

    public Result<Id<Mesh>> Register(MeshData meshData)
    {
        if (AABBHelper.ComputeAABBOfMesh(meshData).TryPickProblems(out var problems, out var aabb))
        {
            return problems.Prepend("Failed to compute AABB for mesh");
        }

        var id = Id.New<Mesh>();
        _meshes[id] = (meshData, aabb);
        OnAdded.Invoke(id);
        return id;
    }

    public DeletionResult Unregister(Id<Mesh> meshId)
    {
        if (!_meshes.ContainsKey(meshId))
        {
            return DeletionResult.NotFound();
        }

        OnRemoved.Invoke(meshId);
        _meshes.Remove(meshId);
        return DeletionResult.Success();
    }

    public bool TryGetMeshData(Id<Mesh> id, [MaybeNullWhen(false)] out MeshData data)
    {
        if (_meshes.TryGetValue(id, out var entry))
        {
            data = entry.Data;
            return true;
        }

        data = null;
        return false;
    }

    public bool TryGetLocalAABB(Id<Mesh> id, out AABB aabb)
    {
        if (_meshes.TryGetValue(id, out var entry))
        {
            aabb = entry.LocalAABB;
            return true;
        }

        aabb = default;
        return false;
    }
}
