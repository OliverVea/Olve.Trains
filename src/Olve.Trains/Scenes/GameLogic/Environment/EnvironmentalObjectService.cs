using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public class EnvironmentalObjectService
{
    private readonly ILogger<EnvironmentalObjectService> _logger;
    private readonly EnvironmentalObjectBlueprintService _blueprintService;
    private readonly EntityStore<EnvironmentalObject> _objects;
    private readonly EntityStoreIndex<EnvironmentalObject, Id<EnvironmentalObjectBlueprint>> _objectsByBlueprint;

    public EnvironmentalObjectService(
        ILogger<EnvironmentalObjectService> logger,
        EnvironmentalObjectBlueprintService blueprintService,
        EntityStoreFactory entityStoreFactory)
    {
        _logger = logger;
        _blueprintService = blueprintService;
        _objects = entityStoreFactory.Create<EnvironmentalObject>();
        _objectsByBlueprint = _objects.CreateIndex(x => x.BlueprintId);
    }

    public Event<Id<EnvironmentalObject>> OnObjectAdded => _objects.OnAdded;
    public Event<Id<EnvironmentalObject>> OnObjectRemoved => _objects.OnRemoved;

    public Result<Id<EnvironmentalObject>> AddObject(
        Id<EnvironmentalObjectBlueprint> blueprintId,
        Position3D position,
        AssetPath<MeshData>? meshPath = null,
        AssetPath<TextureData<RGBA>>? texturePath = null)
    {
        if (!_blueprintService.TryGetBlueprint(blueprintId, out _))
        {
            return new ResultProblem("Environmental object blueprint '{0}' not found", blueprintId);
        }

        EnvironmentalObject obj = new(Id.New<EnvironmentalObject>(), blueprintId, position, meshPath, texturePath);

        if (!_objects.TryAdd(obj))
        {
            return new ResultProblem("Failed to add environmental object '{0}' with blueprint '{1}'", obj.Id, blueprintId);
        }

        _logger.LogInformation("Added environmental object {ObjectId} with blueprint {BlueprintId} at {Position}", obj.Id, blueprintId, position);
        return obj.Id;
    }

    public DeletionResult DeleteObject(Id<EnvironmentalObject> objectId)
    {
        var result = _objects.Remove(objectId);
        if (result.WasNotFound)
        {
            _logger.LogWarning("Tried to delete environmental object {ObjectId} but it was not found", objectId);
        }
        else
        {
            _logger.LogInformation("Deleted environmental object {ObjectId}", objectId);
        }

        return result;
    }

    public Result DeleteObjectsWithBlueprint(Id<EnvironmentalObjectBlueprint> blueprintId)
    {
        foreach (var objectId in _objectsByBlueprint.GetForKey(blueprintId).ToArray())
        {
            var result = DeleteObject(objectId);
            if (result.TryPickProblems(out var problems))
            {
                _logger.Log(problems.Prepend("Failed to delete environmental object '{0}' while deleting all objects with blueprint '{1}'", objectId, blueprintId));
            }
        }

        return Result.Success();
    }

    public bool TryGetObject(Id<EnvironmentalObject> objectId, out EnvironmentalObject obj)
        => _objects.TryGet(objectId, out obj);

    public IEnumerable<Id<EnvironmentalObject>> ObjectIds => _objects.Keys;
    public IEnumerable<EnvironmentalObject> Objects => _objects.Values;

    public IReadOnlyCollection<Id<EnvironmentalObject>> GetObjectsWithBlueprint(Id<EnvironmentalObjectBlueprint> blueprintId)
        => _objectsByBlueprint.GetForKey(blueprintId);
}
