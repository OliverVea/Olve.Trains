using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleApplierService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    GuiAnimationService guiAnimationService,
    GuiStyleRegistry styleRegistry) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([guiAnimationService]);

    private readonly EventQueue<GuiAnimationService.GuiStateWeightsChangedMessage> _guiStateWeightsChangedQueue
        = new(guiAnimationService.GuiStateWeightsChanged);

    protected override Result OnLoad()
    {
        _guiStateWeightsChangedQueue.SetHandler(OnStateWeightsChanged).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return _guiStateWeightsChangedQueue.Update();
    }

    private Result OnStateWeightsChanged(
        GuiAnimationService.GuiStateWeightsChangedMessage message)
    {
        if (!guiElementService.TryGetElement(message.NodeId, out var guiElement))
        {
            return new ResultProblem(
                "Could not find GUI element with node id '{0}'", message.NodeId);
        }

        if (!styleRegistry.TryGetStyle(guiElement, out var style))
        {
            return new ResultProblem(
                "Could not find GUI style for node id '{0}'", message.NodeId);
        }

        if (!style.TryApplyStateWeights(guiElement, message.Weights))
        {
            return new ResultProblem(
                "Failed to apply state weights to GUI element '{0}'", message.NodeId);
        }

        if (guiElement.LayoutBox is not { } layoutBox)
        {
            LoggingManager.Log(
                LogLevel.Warning,
                $"Failed to get LayoutBox for element '{message.NodeId}'. Skipping layout update.");

            return Result.Success();
        }

        return guiLayoutService
            .SetNodeBox(message.NodeId, layoutBox)
            .IfProblem(p =>
                p.Prepend(
                    "Failed to update LayoutBox following GUI element state change '{0}'",
                    message));
    }

}
