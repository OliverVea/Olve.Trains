using System.Globalization;
using System.Text.Json;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Rendering;
using Olve.Trains.Scenes.GameRendering;

namespace Olve.Trains.Commands.GameLogic;

public class ProjectToScreenHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CameraSceneService cameraSceneService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PosArgument = new("pos", "World position (x,y,z)", true);

    public override string Verb => "project-to-screen";
    public override string HelpString => "Projects a 3D world position to normalized screen coordinates (-1 to 1)";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [PosArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(PosArgument).TryPickProblems(out var problems, out var posStr))
        {
            return problems;
        }

        if (posStr.ParseVector3().TryPickProblems(out problems, out var worldPos))
        {
            return problems;
        }

        var camera = cameraSceneService.Camera;

        if (camera.GetViewMatrix().Invert().TryPickProblems(out problems, out var invertedViewMatrix))
        {
            return problems.Prepend("Failed to invert view matrix");
        }

        var invertedRotation = invertedViewMatrix.ExtractRotation();

        var right = Vector3D.Transform(Vector3D<float>.UnitX, invertedRotation);
        var up = Vector3D.Transform(-Vector3D<float>.UnitY, invertedRotation);

        var halfWidth = camera.Projection.OrthographicSize * camera.Projection.AspectRatio;
        var halfHeight = camera.Projection.OrthographicSize;

        var offset = worldPos - camera.View.Position;
        var screenX = Vector3D.Dot(offset, right) / halfWidth;
        var screenY = Vector3D.Dot(offset, up) / halfHeight;

        var json = JsonSerializer.Serialize(new
        {
            x = float.Round(screenX, 6).ToString(CultureInfo.InvariantCulture),
            y = float.Round(screenY, 6).ToString(CultureInfo.InvariantCulture),
        });

        return new CommandOutput(json);
    }
}
