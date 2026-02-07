using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.Tools;

public sealed class ToolManagementService(ILoggingManager loggingManager)
{
    private readonly Dictionary<Id<Tool>, Tool> _tools = [];
    public Id<Tool>? ActiveToolId { get; private set; }
    public Event<ActiveToolChangedMessage> ActiveToolChanged { get; } = new();
    public readonly record struct ActiveToolChangedMessage(Id<Tool>? CurrentTool, Id<Tool>? NewTool);

    public Result AddTool(Tool tool)
    {
        var added = _tools.TryAdd(tool.Id, tool);

        if (added)
        {
            loggingManager.Log(LogLevel.Debug, $"Registered tool '{tool}' to {_tools.Count - 1} tools");
        }
        else
        {
            loggingManager.Log(LogLevel.Warning, $"Failed to add tool '{tool} to {_tools.Count} tools'");
        }

        return added
            ? Result.Success()
            :  new ResultProblem("Tool with id '{0}' already exists.", tool.Id);
    }

    public DeletionResult RemoveTool(Id<Tool> toolId)
    {
        var removed = _tools.Remove(toolId);
        if (!removed)
        {
            var message =
                $"Tool with id '{toolId}' could not be removed as no tool with that id was found among {_tools.Count} tools.";
            loggingManager.Log(LogLevel.Warning, message);
        }

        return removed ? DeletionResult.Success() : DeletionResult.NotFound();
    }

    public Result ToggleActiveTool(Id<Tool> toolId)
    {
        Id<Tool>? newToolId = ActiveToolId == toolId ? null : toolId;
        return SetActiveTool(newToolId);
    }

    public Result SetActiveTool(Id<Tool>? toolId)
    {
        var previousActiveTool = ActiveToolId;

        ActiveToolId = toolId;

        if (toolId != previousActiveTool)
        {
            loggingManager.Log(LogLevel.Debug, $"Changed tool: {previousActiveTool} -> {toolId}");
            ActiveToolChanged.Invoke(new ActiveToolChangedMessage(previousActiveTool, toolId));
        }

        return Result.Success();
    }
}