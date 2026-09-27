using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public class EnvironmentalObjectService
{
    private readonly ILogger<EnvironmentalObjectService> _logger;
    private readonly EnvironmentalObjectBlueprintService _blueprintService;
    private readonly EntityStore<EnvironmentalObject> _objects;
    private readonly EntityStoreOrderedView<EnvironmentalObject, Id<EnvironmentalObject>> _objectsInCreationOrder;
    private readonly SequenceService _sequences;
    private readonly EntityStoreIndex<EnvironmentalObject, Id<EnvironmentalObjectBlueprint>> _objectsByBlueprint;

    public EnvironmentalObjectService(
        ILogger<EnvironmentalObjectService> logger,
        EnvironmentalObjectBlueprintService blueprintService,
        SequenceService sequences,
        EntityStoreFactory entityStoreFactory)
    {
        _logger = logger;
        _blueprintService = blueprintService;
        _sequences = sequences;
        _objects = entityStoreFactory.Create<EnvironmentalObject>();
        _objectsInCreationOrder = _objects.CreateOrderedView(CreationOrder.Of<EnvironmentalObject>());
        _objectsByBlueprint = _objects.CreateIndex(x => x.BlueprintId);
    }

    public Event<EntityAdded<EnvironmentalObject, Id<EnvironmentalObject>>> OnObjectAdded => _objects.OnAdded;
    public Event<EntityDeleted<EnvironmentalObject, Id<EnvironmentalObject>>> OnObjectRemoved => _objects.OnDeleted;

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

        EnvironmentalObject obj = new(Id.New<EnvironmentalObject>(), blueprintId, position, _sequences.Next(), meshPath, texturePath);

        if (!_objects.TryAdd(obj))
        {
            return new ResultProblem("Failed to add environmental object '{0}' with blueprint '{1}'", obj.Id, blueprintId);
        }

        _logger.LogDebug("Added environmental object {ObjectId} with blueprint {BlueprintId} at {Position}", obj.Id, blueprintId, position);
        return obj.Id;
    }

    /// <summary>
    /// Re-adds a fully materialized object from a save, preserving its id (unlike <see cref="AddObject"/>,
    /// which mints a new one). Used by the load path so derived state keyed on the id stays consistent.
    /// Restored objects are numbered afresh in the order they are restored, which is the order they were saved in.
    /// </summary>
    public Result<Id<EnvironmentalObject>> RestoreObject(EnvironmentalObject obj)
    {
        obj = obj with { CreatedSequence = _sequences.Next() };

        if (!_blueprintService.TryGetBlueprint(obj.BlueprintId, out _))
        {
            return new ResultProblem("Environmental object blueprint '{0}' not found", obj.BlueprintId);
        }

        if (!_objects.TryAdd(obj))
        {
            return new ResultProblem("Failed to restore environmental object '{0}' with blueprint '{1}'", obj.Id, obj.BlueprintId);
        }

        _logger.LogDebug("Restored environmental object {ObjectId} with blueprint {BlueprintId} at {Position}", obj.Id, obj.BlueprintId, obj.Position);
        return obj.Id;
    }

    public DeletionResult DeleteObject(Id<EnvironmentalObject> objectId)
    {
        var result = _objects.Delete(objectId);
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

    public IEnumerable<Id<EnvironmentalObject>> ObjectIds => _objectsInCreationOrder.Select(x => x.Id);
    public IEnumerable<EnvironmentalObject> Objects => _objectsInCreationOrder;

    public IReadOnlyCollection<Id<EnvironmentalObject>> GetObjectsWithBlueprint(Id<EnvironmentalObjectBlueprint> blueprintId)
        => _objectsByBlueprint.GetForKey(blueprintId);
}
