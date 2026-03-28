using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Commands.GameLogic;

public class ListTracksHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-tracks";
    public override string HelpString => "Lists all tracks with their endpoints";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var tracks = trackService.Tracks
            .Select(t => new
            {
                trackId = t.Id.ToString(),
                start = FormatEndpoint(t.Start),
                end = FormatEndpoint(t.End),
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new { tracks });
        return new CommandOutput(json);
    }

    private static object FormatEndpoint(TrackEndpoint ep) => new
    {
        x = ep.Point.X.ToString("F3", CultureInfo.InvariantCulture),
        y = ep.Point.Y.ToString("F3", CultureInfo.InvariantCulture),
        z = ep.Point.Z.ToString("F3", CultureInfo.InvariantCulture),
        dirX = ep.Tangent.X.ToString("F3", CultureInfo.InvariantCulture),
        dirY = ep.Tangent.Y.ToString("F3", CultureInfo.InvariantCulture),
        dirZ = ep.Tangent.Z.ToString("F3", CultureInfo.InvariantCulture),
    };
}
