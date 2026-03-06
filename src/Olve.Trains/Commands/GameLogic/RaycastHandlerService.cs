using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Collision;

namespace Olve.Trains.Commands.GameLogic;

public class RaycastHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    CameraSceneService cameraSceneService,
    CollisionSystem collisionSystem) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PosArgument = new("pos", "Normalized screen position (x,y in -1 to 1)", true);

    public override string Verb => "raycast";
    public override string HelpString => "Performs a raycast from screen coordinates and returns collision hits";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [PosArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(PosArgument).TryPickProblems(out var problems, out var posStr))
        {
            return problems;
        }

        if (posStr.ParseVector2().TryPickProblems(out problems, out var screenPos))
        {
            return problems;
        }

        if (cameraSceneService.Camera.GetRay(screenPos).TryPickProblems(out problems, out var ray))
        {
            return problems.Prepend("Failed to create ray from screen position");
        }

        var hits = collisionSystem.Raycast(ray);

        var json = JsonSerializer.Serialize(new
        {
            hits = hits.Select(h => new
            {
                colliderId = h.ColliderId.ToString(),
                group = GroupName(h.Group),
                distance = float.Round(h.Distance, 2).ToString(CultureInfo.InvariantCulture),
            }).ToArray(),
        });

        return new CommandOutput(json);
    }

    private static string GroupName(Id<ColliderGroup> group) =>
        group == ColliderGroups.Signal ? "Signal" :
        group == ColliderGroups.Building ? "Building" :
        group == ColliderGroups.Vehicle ? "Vehicle" :
        group == ColliderGroups.Track ? "Track" :
        group.ToString();
}
