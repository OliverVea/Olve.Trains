using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonBlueprintService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<WagonBlueprint> _blueprints = entityStoreFactory.Create<WagonBlueprint>();

    public Event<Id<WagonBlueprint>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<Id<WagonBlueprint>> OnBlueprintRemoved => _blueprints.OnRemoved;

    public Result Register(WagonBlueprint blueprint)
    {
        if (!_blueprints.TryAdd(blueprint))
        {
            return new ResultProblem("Failed to register wagon blueprint '{0}'", blueprint.Id);
        }

        return Result.Success();
    }

    public bool TryGet(Id<WagonBlueprint> id, out WagonBlueprint blueprint) => _blueprints.TryGet(id, out blueprint);

    public DeletionResult Remove(Id<WagonBlueprint> id) => _blueprints.Remove(id);
}
