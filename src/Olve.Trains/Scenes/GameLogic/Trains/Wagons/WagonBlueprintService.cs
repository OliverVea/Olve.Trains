using System.Diagnostics.CodeAnalysis;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonBlueprintService
{
    private readonly Dictionary<Id<WagonBlueprint>, WagonBlueprint> _blueprints = new();

    public void Register(WagonBlueprint blueprint)
    {
        _blueprints[blueprint.Id] = blueprint;
    }

    public bool TryGet(Id<WagonBlueprint> id, [MaybeNullWhen(false)] out WagonBlueprint blueprint)
    {
        return _blueprints.TryGetValue(id, out blueprint);
    }

    public void Remove(Id<WagonBlueprint> id)
    {
        _blueprints.Remove(id);
    }
}
