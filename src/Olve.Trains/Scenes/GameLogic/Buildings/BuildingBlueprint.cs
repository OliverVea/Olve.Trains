using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public readonly record struct BuildingBlueprint(Id<BuildingBlueprint> Id, string Description, TileFootprint Footprint) : IHasId<Id<BuildingBlueprint>>;
