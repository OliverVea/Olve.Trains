using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class StationCargoTransferService(
    TrainPositionService trainPositionService,
    StationService stationService,
    WagonInventoryService wagonInventoryService,
    CargoInventoryService cargoInventoryService,
    CargoTransferPolicyService cargoTransferPolicyService) : ISceneService
{
    private HashSet<(Id<Train>, Id<Track>)> _activeVisits = [];

    public Result Update(TimeSpan deltaTime)
    {
        var currentVisits = new HashSet<(Id<Train>, Id<Track>)>();

        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            var trackId = trackPosition.TrackId;
            if (!stationService.TryGetStationByTrack(trackId, out var station))
            {
                continue;
            }

            currentVisits.Add((trainId, trackId));

            if (_activeVisits.Contains((trainId, trackId)))
            {
                continue;
            }

            var nearbyIndustries = stationService.GetNearbyIndustries(station.BuildingId).ToList();
            var wagonInventories = wagonInventoryService.GetTrainInventories(trainId).ToList();

            foreach (var wagonInventoryId in wagonInventories)
            {
                foreach (var (cargoTypeId, origin, amount) in cargoInventoryService.GetEntries(wagonInventoryId).ToList())
                {
                    if (amount <= 0) continue;

                    var remaining = amount;
                    foreach (var industry in nearbyIndustries)
                    {
                        var direction = cargoTransferPolicyService.GetDirection(industry.InventoryId, cargoTypeId);
                        if (direction is not (TransferDirection.In or TransferDirection.Both)) continue;

                        var transferred = cargoInventoryService.UpdateWithinCapacity(
                            industry.InventoryId, cargoTypeId, remaining, wagonInventoryId);
                        if (transferred > 0)
                        {
                            cargoInventoryService.UpdateWithinCapacity(
                                wagonInventoryId, cargoTypeId, -transferred, origin);
                            remaining -= transferred;
                            if (remaining <= 0) break;
                        }
                    }
                }
            }

            // Phase 2: LOAD (industry → wagon where direction is Out)
            foreach (var industry in nearbyIndustries)
            {
                foreach (var (cargoTypeId, direction) in cargoTransferPolicyService.GetPolicies(industry.InventoryId))
                {
                    if (direction is not (TransferDirection.Out or TransferDirection.Both)) continue;

                    var available = cargoInventoryService.GetAmount(industry.InventoryId, cargoTypeId);
                    if (available <= 0) continue;

                    foreach (var wagonInventoryId in wagonInventories)
                    {
                        if (!cargoInventoryService.CanAccept(wagonInventoryId, cargoTypeId)) continue;

                        var transferred = cargoInventoryService.UpdateWithinCapacity(
                            wagonInventoryId, cargoTypeId, available, industry.InventoryId);
                        if (transferred > 0)
                        {
                            cargoInventoryService.UpdateWithinCapacity(
                                industry.InventoryId, cargoTypeId, -transferred, industry.InventoryId);
                            available -= transferred;
                            if (available <= 0) break;
                        }
                    }
                }
            }
        }

        _activeVisits = currentVisits;
        return Result.Success();
    }
}
