using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Text;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Generated.Fonts;
using Silk.NET.OpenGL;

namespace Olve.Trains.Shared.GUI;

/// <summary>
/// Handles lifecycle for text elements: loads font atlases and registers/deregisters
/// text with GuiTextRenderingService when GUI elements are added/removed.
/// </summary>
public class GuiTextUpdateService(
    EventQueueFactory eventQueueFactory,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    GuiTextRenderingService textRenderingService,
    Provider<LayoutContext> layoutContextProvider) : ISceneService
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = eventQueueFactory.Create(guiElementService.OnAdded);
    private readonly EventQueue<GuiElementArgs> _elementRemovedQueue = eventQueueFactory.Create(guiElementService.OnRemoved);
    private readonly Dictionary<Id<GuiNode>, Text> _trackedTexts = new();

    public Result Load()
    {
        _elementAddedQueue.SetHandler(OnGuiElementAdded).Init();
        _elementRemovedQueue.SetHandler(OnGuiElementRemoved).Init();

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var (nodeId, _) in _trackedTexts)
        {
            textRenderingService.DeregisterText(nodeId);
        }
        _trackedTexts.Clear();
        _elementAddedQueue.Cleanup();
        _elementRemovedQueue.Cleanup();

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (Result.Chain(_elementAddedQueue.Update, _elementRemovedQueue.Update)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        UpdateDirtyTexts();

        return Result.Success();
    }

    private void UpdateDirtyTexts()
    {
        foreach (var (nodeId, textElement) in _trackedTexts)
        {
            if (textElement.ComputedSize.HasValue) continue;

            var textData = textElement.TextRenderData;
            var font = textData.Font ?? Fonts.RobotoRegular;

            var measuredPx = TextLayoutEngine.MeasureText(textData.Content, font, textData.FontSize);
            var measuredDp = layoutContextProvider.Value.ToDp(measuredPx);
            textElement.ComputedSize = measuredDp;

            // Update layout with the new size
            if (textElement.LayoutBox is { } layoutBox)
            {
                guiLayoutService.SetNodeBox(nodeId, layoutBox);
            }

            textRenderingService.UpdateText(nodeId, textData.Content, textData.FontSize, textData.FontWeight);
        }
    }

    private Result OnGuiElementAdded(GuiElementArgs addedEvent)
    {
        if (!guiElementService.TryGetElement(addedEvent.NodeId, out var element))
        {
            return new ResultProblem("Could not find element with id: {0}", addedEvent.NodeId);
        }

        if (element is not Text textElement)
        {
            return Result.Success();
        }

        _trackedTexts[addedEvent.NodeId] = textElement;

        var textData = textElement.TextRenderData;
        var font = textData.Font ?? Fonts.RobotoRegular;

        var measuredPx = TextLayoutEngine.MeasureText(textData.Content, font, textData.FontSize);
        var measuredDp = layoutContextProvider.Value.ToDp(measuredPx);
        textElement.ComputedSize = measuredDp;

        // Update layout with the computed size
        if (textElement.LayoutBox is { } layoutBox)
        {
            guiLayoutService.SetNodeBox(addedEvent.NodeId, layoutBox);
        }

        if (textureLoadingManager.LoadTexture(font.Atlas.FontAtlas)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load font atlas for text element: {0}", addedEvent.NodeId);
        }

        if (textureEntityManager.Register<RGB, RGBPixelFormat>(textureId, new TextureUploadOptions(Wrap: GLEnum.ClampToEdge, Filter: GLEnum.Linear))
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register font atlas texture with OpenGL: {0}", addedEvent.NodeId);
        }

        return textRenderingService.RegisterText(
            addedEvent.NodeId,
            font,
            textureId,
            textData.Content,
            textData.FontSize,
            textData.FontWeight
        );
    }

    private Result OnGuiElementRemoved(GuiElementArgs removedEvent)
    {
        if (_trackedTexts.Remove(removedEvent.NodeId)) {
            return textRenderingService.DeregisterText(removedEvent.NodeId)
#if DEBUG
                .MapToResult(allowNotFound: false);
#else
                .MapToResult(allowNotFound: true);
#endif
        }

        return Result.Success();
    }
}
