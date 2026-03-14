using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Environment;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameRendering;

public class EnvironmentalObjectRenderingService(
    MeshRenderingService meshRenderingService,
    MeshLoadingManager meshLoadingManager,
    EnvironmentalObjectService environmentalObjectService,
    EnvironmentalObjectBlueprintService blueprintService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    private MeshRenderingService.MeshGroupHandle _fallbackMeshGroup;
    private readonly Dictionary<string, MeshRenderingService.MeshGroupHandle> _meshGroupsByPath = new();
    private readonly Dictionary<Id<EnvironmentalObject>, MeshRenderingService.MeshInstanceHandle> _instances = new();

    public Result Load()
    {
        var vertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(vertices);
        var indices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        if (meshRenderingService.RegisterMeshGroup(
                vertices, indices,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(0.5f, 0.5f, 0.5f)))
            .TryPickProblems(out var problems, out var meshGroup))
        {
            return problems.Prepend("Failed to register environmental object fallback mesh group");
        }

        _fallbackMeshGroup = meshGroup;

        return Result.Success();
    }

    public Result Register(Id<EnvironmentalObject> objectId)
    {
        if (!environmentalObjectService.TryGetObject(objectId, out var obj))
        {
            return new ResultProblem("Environmental object not found: '{0}'", objectId);
        }

        if (GetOrCreateMeshGroup(obj)
            .TryPickProblems(out var problems, out var meshGroup))
        {
            return problems.Prepend("Failed to get mesh group for environmental object '{0}'", objectId);
        }

        var worldMatrix = obj.Position.ToMatrix4X4();

        if (meshRenderingService.AddInstance(meshGroup, worldMatrix)
            .TryPickProblems(out problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add environmental object instance for '{0}'", objectId);
        }

        _instances[objectId] = instanceHandle;
        return Result.Success();
    }

    public Result Unregister(Id<EnvironmentalObject> objectId)
    {
        if (!_instances.Remove(objectId, out var instanceHandle))
        {
            return new ResultProblem("Could not find rendering instance for environmental object '{0}'", objectId);
        }

        if (meshRenderingService.RemoveInstance(instanceHandle)
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to remove environmental object instance for '{0}'", objectId);
        }

        return Result.Success();
    }

    private Result<MeshRenderingService.MeshGroupHandle> GetOrCreateMeshGroup(EnvironmentalObject obj)
    {
        var meshPath = obj.MeshPath;

        if (meshPath is null
            && blueprintService.TryGetBlueprint(obj.BlueprintId, out var blueprint))
        {
            meshPath = blueprint.MeshPath;
        }

        if (meshPath is not { } path)
        {
            return _fallbackMeshGroup;
        }

        var textureKey = obj.TexturePath?.Name ?? "";
        var groupKey = $"{path.Name}:{textureKey}";

        if (_meshGroupsByPath.TryGetValue(groupKey, out var existing))
        {
            return existing;
        }

        if (meshLoadingManager.LoadMesh(path).TryPickProblems(out var loadProblems, out var meshId))
        {
            return loadProblems.Prepend("Failed to load mesh '{0}'", path.Name);
        }

        if (meshRenderingService.RegisterMeshGroup(meshId, obj.TexturePath)
            .TryPickProblems(out var problems, out var meshGroup))
        {
            return problems.Prepend("Failed to register mesh group for '{0}'", path.Name);
        }

        _meshGroupsByPath[groupKey] = meshGroup;
        return meshGroup;
    }
}
