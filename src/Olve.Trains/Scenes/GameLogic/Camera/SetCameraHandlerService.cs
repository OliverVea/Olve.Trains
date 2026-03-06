using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameLogic.Camera;

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
        if (commandContext.GetRequiredArgument(TargetArgument).Bind(s => s.ParseVector3())
            .TryPickProblems(out var problems, out var target))
        {
            return problems;
        }

        if (commandContext.GetRequiredArgument(ZoomArgument).TryPickProblems(out problems, out var zoomArg))
        {
            return problems;
        }

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
}
