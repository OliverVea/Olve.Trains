using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class DeleteVehicleHandlerService(
    ILogger<DeleteVehicleHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    VehicleService vehicleService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument VehicleArgument = new("vehicle", "The ID of the vehicle to delete", true);

    public override string Verb => "delete-vehicle";
    public override string HelpString => "Deletes a vehicle by ID. Example: delete-vehicle vehicle=<vehicle-id>";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [VehicleArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Vehicle>(VehicleArgument).TryPickProblems(out var problems, out var vehicleId))
        {
            return problems;
        }

        var result = vehicleService.DeleteVehicle(vehicleId);
        if (result.MapToResult(allowNotFound: false).TryPickProblems(out problems))
        {
            return problems;
        }

        logger.LogInformation("Deleted vehicle {VehicleId}", vehicleId);
        return new CommandOutput($"Deleted vehicle {vehicleId}");
    }
}
