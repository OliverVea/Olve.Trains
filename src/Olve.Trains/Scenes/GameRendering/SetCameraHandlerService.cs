using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameRendering;

public class SetCameraHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CameraSceneService cameraSceneService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TargetArgument = new("target", "Target position as x,y,z to center the camera on", true);
    private static readonly CommandArgument ZoomArgument = new("zoom", "Number of tiles visible across the X axis", true);

    public override string Verb => "set-camera";
    public override string HelpString => """
        Sets the camera position and zoom level.
        Example: set-camera target=2,0,2 zoom=10
        The zoom value represents the number of tiles visible horizontally.
        """;
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TargetArgument, ZoomArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (TryParseVector3(commandContext.GetArgument(TargetArgument)!, out var target)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        var zoomArg = commandContext.GetArgument(ZoomArgument)!;
        if (!float.TryParse(zoomArg, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
        {
            return new ResultProblem("Could not parse zoom value from '{0}'", zoomArg);
        }

        if (zoom <= 0)
        {
            return new ResultProblem("Zoom must be positive, got '{0}'", zoom);
        }

        cameraSceneService.SetCameraPosition(target, zoom);

        return new CommandOutput($"Camera set to target {target} with zoom {zoom}");
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
