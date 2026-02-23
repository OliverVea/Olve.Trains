using Olve.Engine3D.Commands;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Commands;

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
        if (commandContext.GetRequiredArgument(PosArgument).Bind(s => s.ParseVector2())
            .TryPickProblems(out var problems, out var pos))
        {
            return problems;
        }

        mouseManager.NormalizedPositionOverride = pos;

        return new CommandOutput($"Mouse set to normalized ({pos.X}, {pos.Y})");
    }
}
