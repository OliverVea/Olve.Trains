using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiTexturedRectangleUpdateService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    TextureLoadingService textureLoadingService,
    GuiTexturedRectangleRenderingService texturedRectangleRenderingService) : SceneService(loggingManager)
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

        if (element is not IRenderableAsTexturedRectangle renderableAsTexturedRectangle)
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

        return texturedRectangleRenderingService.RegisterTexturedRectangle(addedEvent.NodeId, textureRenderingId);
    }

    private Result OnGuiElementRemoved(GuiElementArgs removedEvent)
    {
        return texturedRectangleRenderingService.DeregisterTexturedRectangle(removedEvent.NodeId)
#if DEBUG
            .MapToResult(allowNotFound: false);
#else
            .MapToResult(allowNotFound: true);
#endif
    }
}
