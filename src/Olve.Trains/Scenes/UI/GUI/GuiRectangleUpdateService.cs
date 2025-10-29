using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiRectangleUpdateService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    TextureLoadingService textureLoadingService,
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

        if (element is not IRenderableAsRectangle renderableAsTexturedRectangle)
        {
            return Result.Success();
        }

        if (textureLoadingService.LoadTextureOrFallbackIfNull(renderableAsTexturedRectangle.TexturedRectangleData.TexturePath)
            .TryPickProblems(out var problems, out var textureRenderingId))
        {
            return problems.Prepend("Failed to load texture '{0}' for GuiElement: {1}",
                renderableAsTexturedRectangle.TexturedRectangleData,
                addedEvent);
        }

        return rectangleRenderingService.RegisterTexturedRectangle(addedEvent.NodeId, textureRenderingId);
    }

    private Result OnGuiElementRemoved(GuiElementArgs removedEvent)
    {
        return rectangleRenderingService.DeregisterTexturedRectangle(removedEvent.NodeId)
#if DEBUG
            .MapToResult(allowNotFound: false);
#else
            .MapToResult(allowNotFound: true);
#endif
    }
}
