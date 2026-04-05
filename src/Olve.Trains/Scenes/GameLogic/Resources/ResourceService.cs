using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Environment;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameLogic.Resources;

public class ResourceService
{
    private readonly ILogger<ResourceService> _logger;
    private readonly EnvironmentalObjectService _environmentalObjectService;
    private readonly EntityStore<Resource> _resources;
    private readonly EntityStoreIndex<Resource, Id<ResourceType>> _byType;
    private readonly Dictionary<Id<EnvironmentalObject>, Id<Resource>> _byEnvironmentalObject = new();

    private static readonly Dictionary<Id<EnvironmentalObjectBlueprint>, Id<ResourceType>> BlueprintToResourceType = new()
    {
        [EnvironmentalObjectBlueprintCatalog.Tree] = ResourceTypeCatalog.Wood,
    };

    public ResourceService(
        ILogger<ResourceService> logger,
        EnvironmentalObjectService environmentalObjectService,
        EntityStoreFactory entityStoreFactory)
    {
        _logger = logger;
        _environmentalObjectService = environmentalObjectService;
        _resources = entityStoreFactory.Create<Resource>();
        _byType = _resources.CreateIndex(r => r.ResourceTypeId);
    }

    public Event<Id<Resource>> OnResourceAdded => _resources.OnAdded;
    public Event<Id<Resource>> OnResourceRemoved => _resources.OnRemoved;

    public IEnumerable<Resource> Resources => _resources.Values;

    public IReadOnlyCollection<Id<Resource>> GetResourcesOfType(Id<ResourceType> typeId)
        => _byType.GetForKey(typeId);

    public bool TryGetResource(Id<Resource> id, out Resource resource)
        => _resources.TryGet(id, out resource);

    public Result CreateResourceForEnvironmentalObject(Id<EnvironmentalObject> envObjId)
    {
        if (!_environmentalObjectService.TryGetObject(envObjId, out var obj))
        {
            return new ResultProblem("Environmental object not found: '{0}'", envObjId);
        }

        if (!BlueprintToResourceType.TryGetValue(obj.BlueprintId, out var resourceTypeId))
        {
            return Result.Success();
        }

        Resource resource = new(Id.New<Resource>(), resourceTypeId, envObjId, obj.Position.Position);

        if (!_resources.TryAdd(resource))
        {
            return new ResultProblem("Failed to add resource for environmental object '{0}'", envObjId);
        }

        _byEnvironmentalObject[envObjId] = resource.Id;

        _logger.LogDebug("Created resource {ResourceId} of type {ResourceType} for environmental object {EnvObjId}",
            resource.Id, resourceTypeId, envObjId);

        return Result.Success();
    }

    public Result RemoveResourceForEnvironmentalObject(Id<EnvironmentalObject> envObjId)
    {
        if (!_byEnvironmentalObject.Remove(envObjId, out var resourceId))
        {
            return Result.Success();
        }

        var result = _resources.Remove(resourceId);
        if (result.WasNotFound)
        {
            _logger.LogWarning("Resource {ResourceId} for environmental object {EnvObjId} was not found during removal",
                resourceId, envObjId);
        }

        return Result.Success();
    }
}
