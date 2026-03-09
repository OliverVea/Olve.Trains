using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Commands.GameLogic;

public class ListVehiclesHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    VehiclePositionService vehiclePositionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-vehicles";
    public override string HelpString => "Lists all vehicles and their current positions";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var vehicles = vehiclePositionService.TrackPositions
            .Select(tp => new
            {
                vehicleId = tp.Item1.ToString(),
                trackId = tp.Item2.TrackPoint.TrackId.ToString(),
                time = tp.Item2.TrackPoint.Time,
                velocity = tp.Item2.Velocity,
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new { vehicles });
        return new CommandOutput(json);
    }
}
