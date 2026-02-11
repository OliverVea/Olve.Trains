using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;

namespace Olve.Engine3D.GUI.Layout;

public class GuiLayoutUpdateService(
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService
) : ISceneService
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = new(guiElementService.OnRemoved);

    public int Priority => SceneServicePriority.FromDependents([ guiLayoutService ]);

    public Result Load()
    {
        _elementAddedQueue.SetHandler(OnAdded).Init();
        _elementRemovedQueue.SetHandler(OnRemoved).Init();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Chain(_elementAddedQueue.Update, _elementRemovedQueue.Update, guiLayoutService.ComputeLayout);
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

        return guiLayoutService.SetNodeBox(args.NodeId, layoutBox);
    }

    private Result OnRemoved(GuiElementArgs args)
    {
        // guiLayoutService.RemoveBoxForNode(args.NodeId)
        return Result.Success();
    }
}
