using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Commands.GameLogic;

public class QueryVehicleHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument VehicleArgument = new("vehicle", "The vehicle ID to query", true);

    public override string Verb => "query-vehicle";
    public override string HelpString => "Queries the current state of a vehicle";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [VehicleArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Vehicle>(VehicleArgument).TryPickProblems(out var problems, out var vehicleId))
        {
            return problems;
        }

        if (!vehiclePositionService.TryGetTrackPosition(vehicleId, out var trackPosition))
        {
            return new ResultProblem("Vehicle '{0}' has no track position", vehicleId);
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

        var json = JsonSerializer.Serialize(new
        {
            vehicleId = vehicleId.ToString(),
            trackId = trackPosition.TrackPoint.TrackId.ToString(),
            time = trackPosition.TrackPoint.Time,
            velocity = trackPosition.Velocity,
            position,
        });

        return new CommandOutput(json);
    }
}
