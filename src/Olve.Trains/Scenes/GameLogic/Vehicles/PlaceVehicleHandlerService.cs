using System.Globalization;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class PlaceVehicleHandlerService(
    ILogger<PlaceVehicleHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new ("track", "The track to place the vehicle on.", true);
    private static readonly CommandArgument VehicleIdArgument = new("vehicle", "The vehicle to place. If empty, a new vehicle will be created.");
    private static readonly CommandArgument SpeedArgument = new("speed", "The speed of the vehicle.");

    public override string Verb => "place-vehicle";
    public override string HelpString => "Places the specified vehicle on the specified track";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [VehicleIdArgument, TrackArgument, SpeedArgument];
    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var trackIdResult = commandContext.GetId<Track>(TrackArgument);

        var vehicleIdResult = commandContext.Arguments.ContainsKey(VehicleIdArgument.Key)
            ? commandContext.GetId<Vehicle>(VehicleIdArgument)
            : CreateVehicle();

        if (Result.Concat(trackIdResult, vehicleIdResult).TryPickProblems(out var problems, out var trackAndVehicleId))
        {
            return problems;
        }

        var (trackId, vehicleId) = trackAndVehicleId;

        if (!trackService.TrackExists(trackId))
        {
            return new ResultProblem("Track with id '{0}' doesnt exist", trackId);
        }

        var speedString = commandContext.Arguments.GetValueOrDefault(SpeedArgument.Key, "0");
        if (!float.TryParse(speedString, NumberFormatInfo.InvariantInfo, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }

        TrackPoint trackPoint = new(trackId, 0);
        VehicleTrackPosition vehicleTrackPosition = new(trackPoint, speed);

        vehiclePositionService.SetTrackPosition(vehicleId, vehicleTrackPosition);

        logger.LogInformation("Placed vehicle '{VehicleId}' on track with id '{TrackId}' with position '{VehicleTrackPosition}'", vehicleId, trackId, vehicleTrackPosition);

        return CommandOutput.Empty;
    }

    private Result<Id<Vehicle>> CreateVehicle()
    {
        var vehicleName = "Vehicle_" + vehicleService.Count + 1;
        return vehicleService.AddVehicle(vehicleName);
    }
}