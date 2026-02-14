using Olve.Engine3D;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingService
{
    private readonly EntityStore<Building> _buildings = new();
    public Event<Id<Building>> OnBuildingAdded => _buildings.OnAdded;
    public Event<Id<Building>> OnBuildingRemoved => _buildings.OnRemoved;

    public Id<Building> AddBuilding(Id<BuildingBlueprint> blueprintId, TilePosition origin)
    {
        Building building = new(Id.New<Building>(), blueprintId, origin);
        _buildings.TryAdd(building);
        return building.Id;
    }

    public DeletionResult DeleteBuilding(Id<Building> buildingId) => _buildings.Remove(buildingId);
    public bool TryGetBuilding(Id<Building> buildingId, out Building building) => _buildings.TryGet(buildingId, out building);
}