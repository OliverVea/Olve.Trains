using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Logging;
using Olve.Logging;
using Olve.Results;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.CommandHandlers;

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
        var trackIdResult = GetId<Track>(commandContext, TrackArgument);
        
        var vehicleIdResult = commandContext.Arguments.ContainsKey(VehicleIdArgument.Key)
            ? GetId<Vehicle>(commandContext, VehicleIdArgument)
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
        if (!float.TryParse(speedString, out var speed))
        {
            return new ResultProblem("Got invalid speed value '{0}'", speedString);
        }
        TrackPosition trackPosition = new(trackId, 0, speed);
        
        vehiclePositionService.SetTrackPosition(vehicleId, trackPosition);
        
        LoggingManager.Log(LogLevel.Info, $"Placed vehicle '{vehicleId}' on track with id '{trackId}' with position '{trackPosition}'");

        return Result.Success();
    }
    
    private static Result<Id<T>> GetId<T>(CommandContext commandContext, CommandArgument commandArgument)
        => Id<T>.Parse(commandContext.Arguments[commandArgument.Key]);

    private Result<Id<Vehicle>> CreateVehicle()
    {
        var vehicleName = "Vehicle_" + vehicleService.Count + 1;
        return vehicleService.AddVehicle(vehicleName);
    }
}