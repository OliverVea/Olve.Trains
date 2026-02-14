using Olve.Engine3D;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public readonly record struct Building(Id<Building> Id, Id<BuildingBlueprint> BlueprintId, TilePosition Origin) : IHasId<Id<Building>>;