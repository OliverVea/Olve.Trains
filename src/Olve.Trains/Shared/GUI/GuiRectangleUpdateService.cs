using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Shared.GUI;

public class GuiRectangleUpdateService(
    EventQueueFactory eventQueueFactory,
    GuiElementService guiElementService,
    TextureManager textureManager,
    TextureEntityManager textureEntityManager,
    TextureLoadingManager textureLoadingManager,
    GuiRectangleRenderingService rectangleRenderingService) : ISceneService
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = eventQueueFactory.Create(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = eventQueueFactory.Create(guiElementService.OnRemoved);
    private readonly TextureId<RGBA> _singleWhitePixel = textureManager.RegisterTexture(TextureData<RGBA>.Single(RGBA.White));

    public Result Load()
    {
        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(_singleWhitePixel, new TextureUploadOptions())
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to register white pixel texture with OpenGL");
        }

        _elementAddedQueue.SetHandler(OnGuiElementAdded).Init();
        _elementRemovedQueue.SetHandler(OnGuiElementRemoved).Init();

        return Result.Success();
    }

    public Result Unload()
    {
        textureEntityManager.Unregister(_singleWhitePixel);
        _elementAddedQueue.Cleanup();
        _elementRemovedQueue.Cleanup();

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Chain(_elementAddedQueue.Update, _elementRemovedQueue.Update);
    }

    private Result OnGuiElementAdded(GuiElementArgs addedEvent)
    {
        if (!guiElementService.TryGetElement(addedEvent.NodeId, out var element))
        {
            return new ResultProblem("Could not find element with id: {0}", addedEvent.NodeId);
        }

        if (element is not IRenderableAsRectangle renderableAsRectangle)
        {
            return Result.Success();
        }

        var textureId = _singleWhitePixel;

        if (renderableAsRectangle.TexturedRectangleData.TexturePath is { } texturePath)
        {
            if (textureLoadingManager.LoadTexture(texturePath).TryPickProblems(out var problems, out textureId))
            {
                return problems.Prepend("Failed to load texture '{0}' for GuiElement: {1}", texturePath.Path.Path, addedEvent);
            }

            if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(textureId, new TextureUploadOptions())
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to register texture with OpenGL for GuiElement: {0}", addedEvent);
            }
        }

        return rectangleRenderingService.RegisterTexturedRectangle(addedEvent.NodeId, textureId);
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
