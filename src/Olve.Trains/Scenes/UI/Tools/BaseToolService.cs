using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.UI.Tools;

public abstract class BaseToolService<TToolState>(
    ToolManagementService toolManagementService,
    TToolState initialToolState) : ISceneService
{
    public TToolState ToolState { get; protected set; } = initialToolState;

    protected abstract Tool Tool { get; }

    protected virtual TToolState OnToolSelected(TToolState toolState) => toolState;
    protected virtual TToolState OnToolDeselected(TToolState toolState) => toolState;
    protected virtual Result<Pass> OnSelectedInput(TimeSpan deltaTime) => Pass.Pass;
    protected virtual Result OnSelectedUpdate(TimeSpan deltaTime) => Result.Success();
    protected virtual Result OnSelectedRender(TimeSpan deltaTime) => Result.Success();

    public Result Load()
    {
        if (toolManagementService.AddTool(Tool).TryPickProblems(out var problems))
        {
            return problems;
        }

        toolManagementService.ActiveToolChanged.Subscribe(OnActiveToolChanged);
        return Result.Success();
    }

    public Result Unload()
    {
        if (toolManagementService.RemoveTool(Tool.Id).MapToResult(allowNotFound: false).TryPickProblems(out var problems))
        {
            return problems;
        }

        toolManagementService.ActiveToolChanged.Unsubscribe(OnActiveToolChanged);
        return Result.Success();
    }

    public Result<Pass> Input(TimeSpan deltaTime)
        => toolManagementService.ActiveToolId != Tool.Id ? Pass.Pass : OnSelectedInput(deltaTime);

    public Result Update(TimeSpan deltaTime)
        => toolManagementService.ActiveToolId != Tool.Id ? Result.Success() : OnSelectedUpdate(deltaTime);

    public Result Render(TimeSpan deltaTime)
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
