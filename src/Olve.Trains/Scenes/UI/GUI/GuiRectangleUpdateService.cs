using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiRectangleUpdateService(ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiRectangleRenderingService rectangleRenderingService) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = new(guiElementService.OnRemoved);

    protected override Result OnLoad()
    {
        _elementAddedQueue.SetHandler(OnGuiElementAdded).Init();
        _elementRemovedQueue.SetHandler(OnGuiElementRemoved).Init();

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return Result.Chain(_elementAddedQueue.Update, _elementRemovedQueue.Update);
    }

    private Result OnGuiElementAdded(GuiElementArgs addedEvent)
    {
        if (!guiElementService.TryGetElement(addedEvent.NodeId, out var element))
        {
            return new ResultProblem("Could not find element with id: {0}", addedEvent.NodeId);
        }

        return element is IRenderableAsRectangle
            ? rectangleRenderingService.RegisterRectangle(addedEvent.NodeId)
            : Result.Success();
    }

    private Result OnGuiElementRemoved(GuiElementArgs removedEvent)
    {
        return rectangleRenderingService.DeregisterRectangle(removedEvent.NodeId)
#if DEBUG
            .MapToResult(allowNotFound: false);
#else
            .MapToResult(allowNotFound: true);
#endif
    }
}