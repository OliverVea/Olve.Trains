using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling.Animation;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleApplierService(
    ILogger<GuiStyleApplierService> logger,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    GuiStyleRegistry styleRegistry)
{
    public Result ApplyStateWeights(
        GuiAnimationService.GuiStateWeightsChangedMessage message)
    {
        if (!guiElementService.TryGetElement(message.NodeId, out var guiElement))
        {
            logger.LogDebug("Ignoring state weight change for removed node '{NodeId}'", message.NodeId);
            return Result.Success();
        }

        if (!styleRegistry.TryGetStyle(guiElement, out var style))
        {
            return Result.Success();
        }

        if (!style.TryApplyStateWeights(guiElement, message.Weights))
        {
            return new ResultProblem(
                "Failed to apply state weights to GUI element '{0}'", message.NodeId);
        }

        if (guiElement.LayoutBox is not { } layoutBox)
        {
            logger.LogWarning(
                "Failed to get LayoutBox for element '{NodeId}'. Skipping layout update.", message.NodeId);

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
