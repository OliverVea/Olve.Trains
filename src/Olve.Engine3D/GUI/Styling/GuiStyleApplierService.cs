using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleApplierService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiElementStateService stateService,
    GuiLayoutService guiLayoutService,
    GuiStyleRegistry styleRegistry) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementStateService.GuiElementStateChanged> _stateChangedQueue = new(stateService.OnStateChanged);

    protected override Result OnLoad()
    {
        _stateChangedQueue.SetHandler(OnStateChanged).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return _stateChangedQueue.Update();
    }

    private Result OnStateChanged(GuiElementStateService.GuiElementStateChanged args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return new ResultProblem("Could not find GUI element with node id '{0}'", args.NodeId);
        }

        if (element.StyleKey is null)
        {
            return Result.Success();
        }

        if (styleRegistry.ApplyState(element, args.After).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to apply style to GUI element '{0}'", args.NodeId);
        }

        if (element.LayoutBox is not { } layoutBox)
        {
            LoggingManager.Log(LogLevel.Warning, $"Failed to get LayoutBox for element '{args.NodeId}'. Skipping layout update.");
            return Result.Success();
        }

        return guiLayoutService.SetNodeBox(args.NodeId, layoutBox)
            .IfProblem(p => p.Prepend("Failed to update LayoutBox following GUI element state change '{0}'", args));
    }
}
