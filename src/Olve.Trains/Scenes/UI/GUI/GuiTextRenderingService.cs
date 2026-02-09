using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Text;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

/// <summary>
/// Renders text elements using MSDF font atlas rendering.
/// Each text element maps to multiple glyph rendering instances.
/// </summary>
public class GuiTextRenderingService(
    ILoggingManager loggingManager,
    RenderingManager2D renderingManager2D,
    ShaderEntityManager shaderEntityManager,
    Provider<LayoutContext> layoutContext,
    GuiDepthService guiDepthService,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([guiLayoutService]);

    /// <summary>
    /// Tracks one text element's glyph instances and cached layout data.
    /// </summary>
    private readonly record struct TextInstanceData(
        IReadOnlyList<RenderingInstanceId> GlyphInstanceIds,
        TextureId<RGB> FontAtlasId,
        IReadOnlyList<TextLayoutEngine.GlyphLayout> CachedLayout,
        FontData Font,
        string CachedContent,
        float CachedFontSize
    );

    private readonly Dictionary<Id<GuiNode>, TextInstanceData> _instances = new();
    private readonly List<Id<GuiNode>> _nodesToDelete = [];
    private readonly List<ResultProblem> _updateProblems = [];
    private readonly Shaders.MsdfText _shader = new()
    {
        BlendState = RenderState.AlphaBlend,
    };

    protected override Result OnLoad()
    {
        if (shaderEntityManager.Register(_shader.ShaderData)
            .TryPickProblems(out var problems, out var shaderRenderingId))
        {
            return problems;
        }

        _shader.RenderingId = shaderRenderingId;

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        var designSize = layoutContext.Value.ToPx(layoutContext.Value.DesignSize);
        _shader.UResolution = new Vector2D<float>(designSize.X.Value, designSize.Y.Value);

        _nodesToDelete.Clear();
        _updateProblems.Clear();

        foreach (var (nodeId, instanceData) in _instances)
        {
            if (!TryUpdateTextGlyphs(nodeId, instanceData))
            {
                _nodesToDelete.Add(nodeId);
            }
        }

        foreach (var nodeId in _nodesToDelete)
        {
            if (DeregisterText(nodeId).TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
            }
        }

        if (_updateProblems.Any())
        {
            return Result.Failure(_updateProblems.Prepend(
                new ResultProblem("Failed while updating GUI text elements")));
        }

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        if (_instances.Count == 0)
        {
            return Result.Success();
        }

        return renderingManager2D.Render(_shader);
    }

    public Result RegisterText(
        Id<GuiNode> nodeId,
        FontData font,
        TextureId<RGB> fontAtlasId,
        string content,
        float fontSize)
    {
        DeregisterText(nodeId);

        var layout = TextLayoutEngine.ComputeLayout(content, font, fontSize);
        var glyphIds = new List<RenderingInstanceId>();

        foreach (var glyph in layout)
        {
            var glyphInstance = new Shaders.MsdfText.Instance(
                iPosPx: glyph.PositionPx,
                iSizePx: glyph.SizePx,
                iTint: Vector4D<float>.One,
                iUvMin: glyph.UvMin,
                iUvMax: glyph.UvMax);

            var entityParams = new Shaders.MsdfText.EntityParameters(
                UFontAtlas: fontAtlasId
            );

            if (renderingManager2D.Register(_shader.RenderingId, glyphInstance, 0f, entityParams)
                .TryPickProblems(out var problems, out var instanceId))
            {
                foreach (var id in glyphIds)
                    renderingManager2D.Deregister(id);
                return problems.Prepend("Failed to register glyph for text: nodeId={0}", nodeId);
            }

            glyphIds.Add(instanceId);
        }

        _instances[nodeId] = new TextInstanceData(
            glyphIds, fontAtlasId, layout, font, content, fontSize);

        LoggingManager.Log(LogLevel.Debug, $"Registered text rendering for node {nodeId} with {glyphIds.Count} glyphs");

        return Result.Success();
    }

    public DeletionResult DeregisterText(Id<GuiNode> nodeId)
    {
        if (!_instances.Remove(nodeId, out var data))
        {
            return DeletionResult.NotFound();
        }

        foreach (var glyphId in data.GlyphInstanceIds)
        {
            renderingManager2D.Deregister(glyphId);
        }

        LoggingManager.Log(LogLevel.Debug, $"Deregistered text rendering for node {nodeId}");

        return DeletionResult.Success();
    }

    public Result UpdateText(Id<GuiNode> nodeId, string newContent)
    {
        if (!_instances.TryGetValue(nodeId, out var data))
        {
            return Result.Success();
        }

        if (data.CachedContent == newContent)
        {
            return Result.Success();
        }

        var newLayout = TextLayoutEngine.ComputeLayout(newContent, data.Font, data.CachedFontSize);
        var oldGlyphIds = data.GlyphInstanceIds;
        var newGlyphIds = new List<RenderingInstanceId>(oldGlyphIds);

        var entityParams = new Shaders.MsdfText.EntityParameters(UFontAtlas: data.FontAtlasId);

        // Update existing glyphs
        var minCount = int.Min(oldGlyphIds.Count, newLayout.Count);
        for (var i = 0; i < minCount; i++)
        {
            var glyphId = oldGlyphIds[i];
            var glyph = newLayout[i];

            var glyphInstance = new Shaders.MsdfText.Instance(
                iPosPx: glyph.PositionPx,
                iSizePx: glyph.SizePx,
                iTint: Vector4D<float>.One,
                iUvMin: glyph.UvMin,
                iUvMax: glyph.UvMax);

            if (renderingManager2D.Update(glyphId, glyphInstance, 0f, entityParams)
                .TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to update glyph for text: nodeId={0}", nodeId);
            }
        }

        // Register new glyphs if text got longer
        for (var i = oldGlyphIds.Count; i < newLayout.Count; i++)
        {
            var glyph = newLayout[i];

            var glyphInstance = new Shaders.MsdfText.Instance(
                iPosPx: glyph.PositionPx,
                iSizePx: glyph.SizePx,
                iTint: Vector4D<float>.One,
                iUvMin: glyph.UvMin,
                iUvMax: glyph.UvMax);

            if (renderingManager2D.Register(_shader.RenderingId, glyphInstance, 0f, entityParams)
                .TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to register new glyph for text: nodeId={0}", nodeId);
            }

            newGlyphIds.Add(instanceId);
        }

        // Deregister excess glyphs if text got shorter
        for (var i = newLayout.Count; i < oldGlyphIds.Count; i++)
        {
            renderingManager2D.Deregister(oldGlyphIds[i]);
        }

        if (newLayout.Count < oldGlyphIds.Count)
        {
            newGlyphIds.RemoveRange(newLayout.Count, oldGlyphIds.Count - newLayout.Count);
        }

        _instances[nodeId] = data with
        {
            GlyphInstanceIds = newGlyphIds,
            CachedLayout = newLayout,
            CachedContent = newContent
        };

        return Result.Success();
    }

    private bool TryUpdateTextGlyphs(Id<GuiNode> nodeId, TextInstanceData instanceData)
    {
        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            element is not IRenderableAsText textElement)
        {
            return false;
        }

        var textData = textElement.TextRenderData;
        var textOrigin = new Vector2D<float>(
            boxPosition.Position.X.Value,
            boxPosition.Position.Y.Value
        );
        var depth = guiDepthService.GetDepth(nodeId);

        var scale = instanceData.CachedFontSize / instanceData.Font.Metrics.EmSize;
        var baselineOffset = instanceData.Font.Metrics.Ascender * scale;

        for (var i = 0; i < instanceData.GlyphInstanceIds.Count; i++)
        {
            var glyphId = instanceData.GlyphInstanceIds[i];
            var glyphLayout = instanceData.CachedLayout[i];

            var glyphPos = new Vector2D<float>(
                textOrigin.X + glyphLayout.PositionPx.X,
                textOrigin.Y + baselineOffset - glyphLayout.PositionPx.Y - glyphLayout.SizePx.Y
            );

            var glyphInstance = new Shaders.MsdfText.Instance(
                iPosPx: glyphPos,
                iSizePx: glyphLayout.SizePx,
                iTint: textData.Color.ToVector(),
                iUvMin: glyphLayout.UvMin,
                iUvMax: glyphLayout.UvMax);

            var entityParams = new Shaders.MsdfText.EntityParameters(
                UFontAtlas: instanceData.FontAtlasId
            );

            if (renderingManager2D.Update(glyphId, glyphInstance, -depth, entityParams)
                .TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
                return false;
            }
        }

        return true;
    }
}
