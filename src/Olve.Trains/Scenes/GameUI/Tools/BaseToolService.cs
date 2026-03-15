using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameUI.Tools;

public abstract class BaseToolService<TToolState>(
    ToolManagementService toolManagementService,
    TToolState initialToolState) : ISceneService
{
    public TToolState ToolState { get; protected set; } = initialToolState;

    protected abstract Tool Tool { get; }

    protected virtual TToolState OnToolSelected(TToolState toolState) => toolState;
    protected virtual TToolState OnToolDeselected(TToolState toolState) => toolState;
    protected virtual Result<Pass> OnSelectedInput() => Pass.Pass;
    protected virtual Result OnSelectedUpdate() => Result.Success();
    protected virtual Result OnSelectedRender() => Result.Success();

    public virtual Result Load()
    {
        if (toolManagementService.AddTool(Tool).TryPickProblems(out var problems))
        {
            return problems;
        }

        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);
        return Result.Success();
    }

    public virtual Result Unload()
    {
        if (toolManagementService.RemoveTool(Tool.Id).MapToResult(allowNotFound: false).TryPickProblems(out var problems))
        {
            return problems;
        }

        toolManagementService.ActiveToolChanged.Unsubscribe(OnActiveToolChanged);
        return Result.Success();
    }

    public virtual Result<Pass> Input()
        => toolManagementService.ActiveToolId != Tool.Id ? Pass.Pass : OnSelectedInput();

    public virtual Result Update()
        => toolManagementService.ActiveToolId != Tool.Id ? Result.Success() : OnSelectedUpdate();

    public virtual Result Render()
        => toolManagementService.ActiveToolId != Tool.Id ? Result.Success() : OnSelectedRender();

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
