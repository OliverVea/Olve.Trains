using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class QueryTrainHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService,
    TrainWagonService trainWagonService,
    WagonInventoryService wagonInventoryService,
    CargoInventoryService cargoInventoryService,
    CargoTypeService cargoTypeService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrainArgument = new("train", "The train ID to query", true);

    public override string Verb => "query-train";
    public override string HelpString => "Queries the current state of a train";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrainArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Train>(TrainArgument).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        if (!trainPositionService.TryGetTrackPosition(trainId, out var trackPosition))
        {
            return new ResultProblem("Train '{0}' has no track position", trainId);
        }

        object? position = null;
        if (trackSplineService.GetPoint(trackPosition.TrackPoint.TrackId, trackPosition.TrackPoint.Time)
            .TryPickProblems(out _, out var worldPos))
        {
            // Position unavailable — leave null
        }
        else
        {
            position = new
            {
                x = worldPos.X.ToString("F3", CultureInfo.InvariantCulture),
                y = worldPos.Y.ToString("F3", CultureInfo.InvariantCulture),
                z = worldPos.Z.ToString("F3", CultureInfo.InvariantCulture),
            };
        }

        var wagons = trainWagonService.GetWagons(trainId)
            .Select(w =>
            {
                List<object>? cargo = null;
                if (wagonInventoryService.TryGetInventory(w.Id, out var inventoryId))
                {
                    cargo = [];
                    foreach (var (cargoTypeId, _, amount) in cargoInventoryService.GetEntries(inventoryId))
                    {
                        var cargoName = cargoTypeService.TryGetCargoType(cargoTypeId, out var cargoType)
                            ? cargoType.Name
                            : "Unknown";
                        cargo.Add(new { cargoType = cargoName, amount });
                    }
                }

                return new
                {
                    wagonId = w.Id.ToString(),
                    blueprintId = w.BlueprintId.ToString(),
                    cargo = cargo ?? [],
                };
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new
        {
            trainId = trainId.ToString(),
            trackId = trackPosition.TrackPoint.TrackId.ToString(),
            time = trackPosition.TrackPoint.Time,
            velocity = trackPosition.Velocity,
            position,
            wagons,
        });

        return new CommandOutput(json);
    }
}
