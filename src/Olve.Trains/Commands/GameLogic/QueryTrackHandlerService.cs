using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Commands.GameLogic;

public class QueryTrackHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackService trackService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TrackArgument = new("track", "The track ID to query", true);

    public override string Verb => "query-track";
    public override string HelpString => "Queries the endpoints and direction of a track";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TrackArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Track>(TrackArgument).TryPickProblems(out var problems, out var trackId))
        {
            return problems;
        }

        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        var json = JsonSerializer.Serialize(new
        {
            trackId = track.Id.ToString(),
            start = FormatEndpoint(track.Start),
            end = FormatEndpoint(track.End),
        });

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
