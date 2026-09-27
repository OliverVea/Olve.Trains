using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonBlueprintService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<WagonBlueprint> _blueprints = entityStoreFactory.Create<WagonBlueprint>();

    public Event<EntityAdded<WagonBlueprint, Id<WagonBlueprint>>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<EntityDeleted<WagonBlueprint, Id<WagonBlueprint>>> OnBlueprintRemoved => _blueprints.OnDeleted;

    public Result Register(WagonBlueprint blueprint)
    {
        if (!_blueprints.TryAdd(blueprint))
        {
            return new ResultProblem("Failed to register wagon blueprint '{0}'", blueprint.Id);
        }

        return Result.Success();
    }

    public bool TryGet(Id<WagonBlueprint> id, out WagonBlueprint blueprint) => _blueprints.TryGet(id, out blueprint);

    public DeletionResult Remove(Id<WagonBlueprint> id) => _blueprints.Delete(id);
}
