using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Logging;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class QueryGuiHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    GuiElementService guiElementService,
    GuiNodeStateService guiNodeStateService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument IdArgument = new("id", "Element path (e.g. 'DayTimePanel/Dropdown/SkyColorDropdown')", true);

    public override string Verb => "query-gui";
    public override string HelpString => "Queries GUI element state by its layout path";
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

        guiNodeStateService.TryGetState(nodeId, out var state);

        var json = JsonSerializer.Serialize(new
        {
            path,
            nodeId = nodeId.ToString(),
            show = state.HasFlag(GuiNodeState.Show),
            enabled = state.HasFlag(GuiNodeState.Enabled),
            focused = state.HasFlag(GuiNodeState.Focused),
            pressed = state.HasFlag(GuiNodeState.Pressed),
            active = state.HasFlag(GuiNodeState.Active),
        });

        return new CommandOutput(json);
    }
}
