using Olve.Engine3D.Commands;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.Logging;
using Olve.Trains.Commands.GameLogic;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class ActivateGuiHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument IdArgument = new("id", "Element path (e.g. 'InfoBar/Box/MenuButton')", true);

    public override string Verb => "activate-gui";
    public override string HelpString => "Activates a GUI element by its layout path (e.g. activate-gui id=InfoBar/Box/MenuButton)";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [IdArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(IdArgument).TryPickProblems(out var problems, out var path))
        {
            return problems;
        }

        var elementId = Id.FromName<GuiElement>(path);

        if (!guiElementService.TryGetAnyGuiNodeId(elementId, out var nodeId))
        {
            return new ResultProblem("No registered GUI element found for path '{0}'", path);
        }

        guiActivationService.Activate(nodeId);

        return new CommandOutput($"Activated GUI element: {path}");
    }
}
