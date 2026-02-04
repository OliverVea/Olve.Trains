using System.Globalization;
using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Logging;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class PlaceVehicleHandlerService(
    ILoggingManager loggingManager,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService) : CommandHandlerService(loggingManager, commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new ("track", "The track to place the vehicle on.", true);
    private static readonly CommandArgument VehicleIdArgument = new("vehicle", "The vehicle to place. If empty, a new vehicle will be created.");
    private static readonly CommandArgument SpeedArgument = new("speed", "The speed of the vehicle.");

    public override string Verb => "place-vehicle";
    public override string HelpString => "Places the specified vehicle on the specified track";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [VehicleIdArgument, TrackArgument, SpeedArgument];
    public override Result Handle(CommandContext commandContext)
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

        if (!trackService.Exists(trackId))
        {
            return new ResultProblem("Track with id '{0}' doesnt exist", trackId);
        }

        var speedString = commandContext.Arguments.GetValueOrDefault(SpeedArgument.Key, "0");
        if (!float.TryParse(speedString, NumberFormatInfo.InvariantInfo, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }
        VehicleTrackPosition vehicleTrackPosition = new(trackId, 0, speed);

        vehiclePositionService.SetTrackPosition(vehicleId, vehicleTrackPosition);

        LoggingManager.Log(LogLevel.Info, $"Placed vehicle '{vehicleId}' on track with id '{trackId}' with position '{vehicleTrackPosition}'");

        return Result.Success();
    }

    private Result<Id<Vehicle>> CreateVehicle()
    {
        var vehicleName = "Vehicle_" + vehicleService.Count + 1;
        return vehicleService.AddVehicle(vehicleName);
    }
}