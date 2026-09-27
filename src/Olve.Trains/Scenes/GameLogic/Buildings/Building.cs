using Olve.Engine3D;
using Olve.Utilities.Lookup;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public readonly record struct BuildingPosition(
    TilePosition BottomLeft,
    CardinalDirection CardinalDirection = CardinalDirection.North);

public readonly record struct Building(Id<Building> Id, Id<BuildingBlueprint> BlueprintId, BuildingPosition Position, long CreatedSequence)
    : IHasId<Id<Building>>, IHasCreatedSequence;