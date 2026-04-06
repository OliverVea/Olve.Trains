using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Cities;

namespace Olve.Trains.Commands.GameLogic;

public class ListCitiesHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CityService cityService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-cities";
    public override string HelpString => "Lists all cities with their residences";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var residencesByCity = cityService.Residences
            .GroupBy(r => r.CityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var cities = cityService.Cities
            .Select(c =>
            {
                var residences = residencesByCity.TryGetValue(c.Id, out var list) ? list : [];
                return new
                {
                    cityId = c.Id.ToString(),
                    residenceCount = residences.Count,
                    residences = residences.Select(r => new
                    {
                        residenceId = r.Id.ToString(),
                        buildingId = r.BuildingId.ToString(),
                    }).ToArray(),
                };
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new { cities });
        return new CommandOutput(json);
    }
}
