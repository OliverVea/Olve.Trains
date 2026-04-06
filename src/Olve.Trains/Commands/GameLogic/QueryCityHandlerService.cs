using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Cities;

namespace Olve.Trains.Commands.GameLogic;

public class QueryCityHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CityService cityService,
    CargoInventoryService cargoInventoryService,
    CargoTypeService cargoTypeService,
    CargoTransferPolicyService cargoTransferPolicyService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument CityArgument = new("city", "The city ID to query", true);

    public override string Verb => "query-city";
    public override string HelpString => "Queries the current state of a city, including its inventory and residences";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [CityArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<City>(CityArgument).TryPickProblems(out var problems, out var cityId))
        {
            return problems;
        }

        if (!cityService.TryGetCity(cityId, out var city))
        {
            return new ResultProblem("City not found: '{0}'", cityId);
        }

        var inventoryItems = new List<object>();
        foreach (var (cargoTypeId, direction) in cargoTransferPolicyService.GetPolicies(city.InventoryId))
        {
            var cargoName = cargoTypeService.TryGetCargoType(cargoTypeId, out var cargoType)
                ? cargoType.Name
                : "Unknown";

            var amount = cargoInventoryService.GetAmount(city.InventoryId, cargoTypeId);
            var capacity = cargoInventoryService.GetRemainingCapacityForType(city.InventoryId, cargoTypeId) + amount;

            inventoryItems.Add(new
            {
                cargoType = cargoName,
                amount,
                capacity,
                direction = direction.ToString(),
            });
        }

        var residences = cityService.Residences
            .Where(r => r.CityId == cityId)
            .Select(r => new
            {
                residenceId = r.Id.ToString(),
                buildingId = r.BuildingId.ToString(),
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new
        {
            cityId = city.Id.ToString(),
            inventory = inventoryItems,
            residences,
        });

        return new CommandOutput(json);
    }
}
