using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Commands.GameLogic;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class SelectToolHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    ToolManagementService toolManagementService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument NameArgument = new("name", "Tool name (e.g. 'Place Tracks', 'Place Station', 'Place Train', 'Delete')", true);

    public override string Verb => "select-tool";
    public override string HelpString => "Selects a tool by name. Use name=none to deselect. Example: select-tool name=Place Tracks";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [NameArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(NameArgument).TryPickProblems(out var problems, out var name))
        {
            return problems;
        }

        if (name.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            if (toolManagementService.SetActiveTool(null).TryPickProblems(out problems))
            {
                return problems;
            }

            return new CommandOutput("Deselected tool");
        }

        if (!toolManagementService.TryGetToolByName(name, out var tool))
        {
            var available = string.Join(", ", toolManagementService.ToolNames);
            return new ResultProblem("Tool '{0}' not found. Available tools: {1}", name, available);
        }

        if (toolManagementService.SetActiveTool(tool.Id).TryPickProblems(out problems))
        {
            return problems;
        }

        return new CommandOutput($"Selected tool: {tool.Name}");
    }
}
