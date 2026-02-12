using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameUI.Tools;

public sealed class ToolManagementService(ILogger<ToolManagementService> logger)
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
            logger.LogDebug("Registered tool '{Tool}' to {ToolCount} tools", tool, _tools.Count - 1);
        }
        else
        {
            logger.LogWarning("Failed to add tool '{Tool}' to {ToolCount} tools", tool, _tools.Count);
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
            logger.LogWarning("Tool with id '{ToolId}' could not be removed as no tool with that id was found among {ToolCount} tools", toolId, _tools.Count);
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
            logger.LogDebug("Changed tool: {PreviousActiveTool} -> {NewToolId}", previousActiveTool, toolId);
            ActiveToolChanged.Invoke(new ActiveToolChangedMessage(previousActiveTool, toolId));
        }

        return Result.Success();
    }
}