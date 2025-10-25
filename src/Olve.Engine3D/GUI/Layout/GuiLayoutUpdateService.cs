using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Engine3D.GUI.Layout;

public class GuiLayoutUpdateService(
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    ILoggingManager loggingManager
) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = new(guiElementService.OnRemoved);

    public override int Priority => GetPriorityFromDependents([ guiLayoutService ]);

    protected override Result OnLoad()
    {
        _elementAddedQueue.SetHandler(OnAdded).Init();
        _elementRemovedQueue.SetHandler(OnRemoved).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return Result.Chain(_elementAddedQueue.Update, _elementRemovedQueue.Update);
    }

    private Result OnAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return new ResultProblem("Could not get GUI element to remove");
        }

        if (element.LayoutBox is not { } layoutBox)
        {
            return Result.Success();
        }

        guiLayoutService.CreateOrSetNodeBox(args.NodeId, layoutBox);

        return Result.Success();
    }

    private Result OnRemoved(GuiElementArgs args)
    {
        // guiLayoutService.RemoveBoxForNode(args.NodeId)
        return Result.Success();
    }
}