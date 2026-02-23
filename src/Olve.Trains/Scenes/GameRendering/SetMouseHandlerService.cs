using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameRendering;

public class SetMouseHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    MouseManager mouseManager) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PosArgument = new("pos", "Normalized mouse position as x,y in range -1 to 1 (0,0 = center)", true);

    public override string Verb => "set-mouse";
    public override string HelpString => "Sets the normalized mouse position (-1 to 1). Example: set-mouse pos=0,0 (center of screen)";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [PosArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var posArg = commandContext.GetArgument(PosArgument)!;
        var parts = posArg.Split(',');

        if (parts.Length != 2
            || !float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return new ResultProblem("Expected 2 comma-separated values (x,y), got '{0}'", posArg);
        }

        mouseManager.NormalizedPositionOverride = new Vector2D<float>(x, y);

        return new CommandOutput($"Mouse set to normalized ({x}, {y})");
    }
}
