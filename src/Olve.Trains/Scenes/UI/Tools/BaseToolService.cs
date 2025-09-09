using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.Tools;

public abstract class BaseToolService<TToolState>(
    ILoggingManager loggingManager,
    ToolManagementService toolManagementService,
    TToolState initialToolState) : SceneService(loggingManager)
{
    public TToolState ToolState { get; protected set; } = initialToolState;

    protected abstract Tool Tool { get; }

    protected virtual TToolState OnToolSelected(TToolState toolState)
    {
        return toolState;
    }

    protected virtual TToolState OnToolDeselected(TToolState toolState)
    {
        return toolState;
    }

    protected virtual Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        return Pass.Pass;
    }

    protected virtual Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    protected virtual Result OnSelectedRender(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    protected override Result OnLoad()
    {
        if (toolManagementService.AddTool(Tool).TryPickProblems(out var problems))
        {
            return problems;
        }
        
        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        if (toolManagementService.RemoveTool(Tool.Id).MapToResult(allowNotFound: false).TryPickProblems(out var problems))
        {
            return problems;
        }
        
        toolManagementService.ActiveToolChanged.Unsubscribe(OnActiveToolChanged);
        return Result.Success();
    }

    protected override Result<Pass> OnInput(TimeSpan deltaTime)
        => toolManagementService.ActiveToolId != Tool.Id ? Pass.Pass : OnSelectedInput(deltaTime);

    protected override Result OnUpdate(TimeSpan deltaTime)
        => toolManagementService.ActiveToolId != Tool.Id ? Result.Success() : OnSelectedUpdate(deltaTime);

    protected override Result OnRender(TimeSpan deltaTime)
        => toolManagementService.ActiveToolId != Tool.Id ? Result.Success() : OnSelectedRender(deltaTime);

    private void OnActiveToolChanged(ToolManagementService.ActiveToolChangedMessage activeToolChangedMessage)
    {
        if (activeToolChangedMessage.NewTool == Tool.Id)
        {
            ToolState = OnToolSelected(ToolState);
        }
        
        if (activeToolChangedMessage.CurrentTool == Tool.Id)
        {
            ToolState = OnToolDeselected(ToolState);
        }
    }
}