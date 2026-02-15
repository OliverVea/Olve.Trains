using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public readonly record struct BuildingBlueprint(Id<BuildingBlueprint> Id, string Description, TileFootprint Footprint, BuildingType BuildingType) : IHasId<Id<BuildingBlueprint>>;

public enum BuildingType
{
    None = 0,
    Station,
}