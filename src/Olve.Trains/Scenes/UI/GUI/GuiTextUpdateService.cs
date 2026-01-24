using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Generated.Fonts;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI
{
    /// <summary>
    /// Handles lifecycle for text elements: loads font atlases and registers/deregisters
    /// text with GuiTextRenderingService when GUI elements are added/removed.
    /// </summary>
    public class GuiTextUpdateService(
        ILoggingManager loggingManager,
        GuiElementService guiElementService,
        TextureLoadingService textureLoadingService,
        GuiTextRenderingService textRenderingService) : SceneService(loggingManager)
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

            if (element is not IRenderableAsText textElement)
            {
                return Result.Success();
            }

            var textData = textElement.TextRenderData;
            var font = textData.Font ?? Fonts.RobotoRegular;

            if (textureLoadingService.LoadTexture(font.Atlas.FontAtlas)
                .TryPickProblems(out var problems, out var textureId))
            {
                return problems.Prepend("Failed to load font atlas for text element: {0}", addedEvent.NodeId);
            }

            return textRenderingService.RegisterText(
                addedEvent.NodeId,
                font,
                textureId,
                textData.Content,
                textData.FontSize
            );
        }

        private Result OnGuiElementRemoved(GuiElementArgs removedEvent)
        {
            return textRenderingService.DeregisterText(removedEvent.NodeId)
#if DEBUG
                .MapToResult(allowNotFound: false);
#else
            .MapToResult(allowNotFound: true);
#endif
        }
    }
}
