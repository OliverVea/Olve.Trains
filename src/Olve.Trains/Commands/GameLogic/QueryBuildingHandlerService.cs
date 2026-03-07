using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Commands.GameLogic;

public class QueryBuildingHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    IndustryService industryService,
    IndustryRecipeService industryRecipeService,
    CargoInventoryService cargoInventoryService,
    CargoTypeService cargoTypeService,
    CargoTransferPolicyService cargoTransferPolicyService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument BuildingArgument = new("building", "The building ID to query", true);

    public override string Verb => "query-building";
    public override string HelpString => "Queries the current state of a building, including industry inventory";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [BuildingArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Building>(BuildingArgument).TryPickProblems(out var problems, out var buildingId))
        {
            return problems;
        }

        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        var blueprintName = buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint)
            ? blueprint.Description
            : "Unknown";

        object? industryData = null;
        if (industryService.TryGetByBuilding(buildingId, out var industry))
        {
            var recipeName = industryRecipeService.TryGetRecipe(industry.RecipeId, out var recipe)
                ? recipe.Name
                : "Unknown";

            var inventoryItems = new List<object>();
            foreach (var (cargoTypeId, direction) in cargoTransferPolicyService.GetPolicies(industry.InventoryId))
            {
                var cargoName = cargoTypeService.TryGetCargoType(cargoTypeId, out var cargoType)
                    ? cargoType.Name
                    : "Unknown";

                var amount = cargoInventoryService.GetAmount(industry.InventoryId, cargoTypeId);
                var capacity = cargoInventoryService.GetRemainingCapacityForType(industry.InventoryId, cargoTypeId) + amount;

                inventoryItems.Add(new
                {
                    cargoType = cargoName,
                    amount,
                    capacity,
                    direction = direction.ToString(),
                });
            }

            industryData = new
            {
                recipe = recipeName,
                inventory = inventoryItems,
            };
        }

        var json = JsonSerializer.Serialize(new
        {
            buildingId = buildingId.ToString(),
            type = blueprintName,
            industry = industryData,
        });

        return new CommandOutput(json);
    }
}
