using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Text;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;
using Olve.Utilities.Ids;

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
        Id<Texture> FontAtlasId,
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

    // ─────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────

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
        _shader.UResolution = layoutContext.Value.ViewportSize.As<float>();

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

    // ─────────────────────────────────────────────────────────────────
    // REGISTRATION
    // ─────────────────────────────────────────────────────────────────

    public Result RegisterText(
        Id<GuiNode> nodeId,
        FontData font,
        Id<Texture> fontAtlasId,
        string content,
        float fontSize)
    {
        // Deregister existing if any
        DeregisterText(nodeId);

        // Compute glyph layout
        var layout = TextLayoutEngine.ComputeLayout(content, font, fontSize);
        var glyphIds = new List<RenderingInstanceId>();

        // Set shader parameters
        _shader.UPxRange = font.Atlas.DistanceRange;

        // Create a rendering instance for each glyph
        foreach (var glyph in layout)
        {
            var rectData = new RectangleData
            {
                PositionPx = glyph.PositionPx,  // Will be offset in OnUpdate
                SizePx = glyph.SizePx,
                TintRgba = Vector4D<float>.One,  // Will be set from element
                UvMin = glyph.UvMin,
                UvMax = glyph.UvMax,
                Depth = 0  // Will be set in OnUpdate
            };

            var entityParams = new Shaders.MsdfText.EntityParameters(
                UFontAtlas: fontAtlasId
            );

            if (renderingManager2D.RegisterGlyph(_shader.RenderingId, rectData, entityParams)
                .TryPickProblems(out var problems, out var instanceId))
            {
                // Cleanup on failure
                foreach (var id in glyphIds)
                    renderingManager2D.DeregisterRectangle(id);
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
            renderingManager2D.DeregisterRectangle(glyphId);
        }

        LoggingManager.Log(LogLevel.Debug, $"Deregistered text rendering for node {nodeId}");

        return DeletionResult.Success();
    }

    // ─────────────────────────────────────────────────────────────────
    // UPDATE HELPERS
    // ─────────────────────────────────────────────────────────────────

    private bool TryUpdateTextGlyphs(Id<GuiNode> nodeId, TextInstanceData instanceData)
    {
        // Get layout position, element data, and depth
        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            !guiDepthService.GetDepth(nodeId).TryPickValue(out var depth) ||
            element is not IRenderableAsText textElement)
        {
            return false;
        }

        var textData = textElement.TextRenderData;
        var textOrigin = new Vector2D<float>(
            boxPosition.Position.X.Value,
            boxPosition.Position.Y.Value
        );

        // The baseline offset: place text so baseline aligns with the box
        // For top-aligned text, we offset by the ascender
        var baselineOffset = instanceData.Font.Metrics.Ascender * instanceData.CachedFontSize;

        // Update each glyph's position (text origin + glyph offset)
        for (int i = 0; i < instanceData.GlyphInstanceIds.Count; i++)
        {
            var glyphId = instanceData.GlyphInstanceIds[i];
            var glyphLayout = instanceData.CachedLayout[i];

            // Glyph position: text origin + glyph offset
            // Note: PlaneBounds.Y is typically negative (below baseline) for most glyphs
            // We need to flip the Y positioning since screen Y goes down
            var glyphPos = new Vector2D<float>(
                textOrigin.X + glyphLayout.PositionPx.X,
                textOrigin.Y + baselineOffset - glyphLayout.PositionPx.Y - glyphLayout.SizePx.Y
            );

            var rectData = new RectangleData
            {
                PositionPx = glyphPos,
                SizePx = glyphLayout.SizePx,
                TintRgba = textData.Color,
                UvMin = glyphLayout.UvMin,
                UvMax = glyphLayout.UvMax,
                Depth = -depth
            };

            var entityParams = new Shaders.MsdfText.EntityParameters(
                UFontAtlas: instanceData.FontAtlasId
            );

            if (renderingManager2D.UpdateGlyph(glyphId, rectData, entityParams)
                .TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
                return false;
            }
        }

        return true;
    }
}
