using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;

namespace Olve.Trains.Commands.GameLogic;

public class SetWagonFilterHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    ILogger<SetWagonFilterHandlerService> logger,
    WagonInventoryService wagonInventoryService,
    CargoTransferPolicyService cargoTransferPolicyService,
    CargoTypeService cargoTypeService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument WagonArgument = new("wagon", "The wagon ID", true);
    private static readonly CommandArgument TypeArgument = new("type", "The cargo type name (e.g. Wood, Coal, Planks)", true);
    private static readonly CommandArgument DirectionArgument = new("direction", "Transfer direction: In, Out, Both, or None (to remove)", false);

    public override string Verb => "set-wagon-filter";
    public override string HelpString => "Sets a transfer policy on a wagon's inventory (controls what cargo it accepts/provides)";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [WagonArgument, TypeArgument, DirectionArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Wagon>(WagonArgument).TryPickProblems(out var problems, out var wagonId))
        {
            return problems;
        }

        if (!wagonInventoryService.TryGetInventory(wagonId, out var inventoryId))
        {
            return new ResultProblem("Wagon inventory not found: '{0}'", wagonId);
        }

        if (commandContext.GetRequiredArgument(TypeArgument).TryPickProblems(out problems, out var typeNameValue))
        {
            return problems;
        }

        var cargoType = cargoTypeService.CargoTypes
            .FirstOrDefault(ct => string.Equals(ct.Name, typeNameValue, StringComparison.OrdinalIgnoreCase));
        if (cargoType.Id == default)
        {
            return new ResultProblem("Unknown cargo type: '{0}'", typeNameValue);
        }

        var cargoTypeId = cargoType.Id;

        var directionStr = commandContext.GetOptionalArgument(DirectionArgument) ?? "In";
        if (!Enum.TryParse<TransferDirection>(directionStr, ignoreCase: true, out var direction))
        {
            return new ResultProblem("Invalid direction: '{0}'. Use In, Out, Both, or None", directionStr);
        }

        cargoTransferPolicyService.SetPolicy(inventoryId, cargoTypeId, direction);

        logger.LogInformation(
            "Set wagon {WagonId} filter: {CargoType} = {Direction}",
            wagonId, typeNameValue, direction);

        return new CommandOutput($"Set filter on wagon {wagonId}: {typeNameValue} = {direction}");
    }
}
