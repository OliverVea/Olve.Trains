using System.Globalization;
using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class PlaceTrackHandlerService(
    ILogger<PlaceTrackHandlerService> logger,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    TrackPlacingService trackPlacingService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument StartArgument = new("start", "Start position as x,y,z", true);
    private static readonly CommandArgument EndArgument = new("end", "End position as x,y,z", true);

    public override string Verb => "place-track";
    public override string HelpString => "Places a straight track between two positions";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [StartArgument, EndArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (TryParseVector3(commandContext.GetArgument(StartArgument)!, out var start)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        if (TryParseVector3(commandContext.GetArgument(EndArgument)!, out var end)
            .TryPickProblems(out problems))
        {
            return problems;
        }

        var direction = Vector3D.Normalize(end - start);
        var startEndpoint = new TrackEndpoint(start, direction);
        var endEndpoint = new TrackEndpoint(end, direction);

        if (trackPlacingService.PlaceTrack(startEndpoint, endEndpoint)
            .TryPickProblems(out problems, out var trackIds))
        {
            return problems;
        }

        logger.LogInformation("Placed {Count} track segments from {Start} to {End}", trackIds.Count, start, end);
        var idList = string.Join("\n", trackIds);
        return new CommandOutput($"Placed {trackIds.Count} track segments:\n{idList}");
    }

    private static Result TryParseVector3(string input, out Vector3D<float> result)
    {
        result = default;
        var parts = input.Split(',');
        if (parts.Length != 3)
        {
            return new ResultProblem("Expected 3 comma-separated values (x,y,z), got '{0}'", input);
        }

        if (!float.TryParse(parts[0].Trim(), NumberFormatInfo.InvariantInfo, out var x)
            || !float.TryParse(parts[1].Trim(), NumberFormatInfo.InvariantInfo, out var y)
            || !float.TryParse(parts[2].Trim(), NumberFormatInfo.InvariantInfo, out var z))
        {
            return new ResultProblem("Could not parse coordinates from '{0}'", input);
        }

        result = new Vector3D<float>(x, y, z);
        return Result.Success();
    }
}