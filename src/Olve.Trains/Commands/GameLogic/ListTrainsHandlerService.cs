using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Commands.GameLogic;

public class ListTrainsHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrainPositionService trainPositionService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-trains";
    public override string HelpString => "Lists all trains and their current positions";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var trains = trainPositionService.TrackPositions
            .Select(tp => new
            {
                trainId = tp.Item1.ToString(),
                trackId = tp.Item2.TrackPoint.TrackId.ToString(),
                time = tp.Item2.TrackPoint.Time,
                velocity = tp.Item2.Velocity,
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new { trains });
        return new CommandOutput(json);
    }
}
