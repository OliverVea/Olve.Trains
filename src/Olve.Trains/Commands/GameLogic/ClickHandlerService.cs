using Olve.Engine3D.Commands;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Silk.NET.Input;

namespace Olve.Trains.Commands.GameLogic;

public class ClickHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    MouseManager mouseManager) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument PosArgument = new("pos", "Normalized screen position as x,y in range -1 to 1 (0,0 = center)", true);

    public override string Verb => "click";
    public override string HelpString => "Simulates a left-click at the given normalized position. Example: click pos=0.5,0.3";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [PosArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(PosArgument).Bind(s => s.ParseVector2())
            .TryPickProblems(out var problems, out var pos))
        {
            return problems;
        }

        if (float.IsNaN(pos.X) || float.IsNaN(pos.Y))
        {
            return new ResultProblem("Click position contains NaN ({0}, {1})", pos.X, pos.Y);
        }

        mouseManager.NormalizedPositionOverride = pos;
        mouseManager.SimulateClick(MouseButton.Left);

        return new CommandOutput($"Click simulated at ({pos.X}, {pos.Y})");
    }
}
