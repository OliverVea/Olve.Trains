using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Text;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Shared.GUI;

public class GuiTextRenderingService(
    ILogger<GuiTextRenderingService> logger,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    Provider<LayoutContext> layoutContext,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    GuiDepthService guiDepthService,
    GuiNodeStateService guiNodeStateService,
    SharedRenderingService sharedRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([guiLayoutService]);

    private static readonly RenderState GuiRenderState = new(BlendMode.Alpha, DepthWrite: false, DepthTest: false);
    private const int GuiSortKey = 1001;

    private readonly record struct TextGroupKey(UntypedTextureId FontAtlasId, float FontWeight, int Depth);

    /// <summary>
    /// Tracks one text element's glyph instances and cached layout data.
    /// </summary>
    private readonly record struct TextInstanceData(
        IReadOnlyList<Id<Shaders.MsdfText.Instance>> GlyphInstanceIds,
        TextGroupKey GroupKey,
        IReadOnlyList<TextLayoutEngine.GlyphLayout> CachedLayout,
        FontData Font,
        string CachedContent,
        float CachedFontSize,
        float CachedFontWeight
    );

    private readonly Dictionary<Id<GuiNode>, TextInstanceData> _instances = new();
    private readonly Dictionary<TextGroupKey, GroupId<Shaders.MsdfText.Instance>> _textGroups = new();
    private readonly List<Id<GuiNode>> _nodesToDelete = [];
    private readonly List<ResultProblem> _updateProblems = [];
    private readonly Shaders.MsdfText _shader = new()
    {
        BlendState = RenderState.AlphaBlendNoDepthWrite,
    };

    private GeometryId<Shaders.MsdfText.Vertex> _quadGeometryId = null!;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load MSDF text shader");
        }

        var quadVertices = new Shaders.MsdfText.Vertex[UnitQuad.VertexCount];
        UnitQuad.Populate(quadVertices);

        if (geometryManager.Register(quadVertices, ReadOnlySpan<uint>.Empty)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register text quad geometry");
        }

        _quadGeometryId = geometryId;

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var (nodeId, _) in _instances)
        {
            if (DeregisterText(nodeId).TryPickProblems(out var problems))
            {
                logger.Log(problems);
            }
        }

        return Result.Success();
    }

    public Result Update()
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

    public Result RegisterText(
        Id<GuiNode> nodeId,
        FontData font,
        TextureId<RGB> fontAtlasId,
        string content,
        float fontSize,
        float fontWeight)
    {
        DeregisterText(nodeId);

        var layout = TextLayoutEngine.ComputeLayout(content, font, fontSize);
        var depth = guiDepthService.GetDepth(nodeId);
        var groupKey = new TextGroupKey(fontAtlasId, fontWeight, depth);
        var groupId = GetOrCreateGroup(groupKey, fontAtlasId, fontWeight);
        var glyphIds = new List<Id<Shaders.MsdfText.Instance>>();

        foreach (var glyph in layout)
        {
            var glyphInstance = new Shaders.MsdfText.Instance(
                iPosPx: glyph.PositionPx,
                iSizePx: glyph.SizePx,
                iTint: Vector4D<float>.One,
                iUvMin: glyph.UvMin,
                iUvMax: glyph.UvMax);

            if (renderingInstanceManager.Add(groupId, glyphInstance)
                .TryPickProblems(out var problems, out var instanceId))
            {
                foreach (var id in glyphIds)
                    renderingInstanceManager.Remove(groupId, id);
                return problems.Prepend("Failed to register glyph for text: nodeId={0}", nodeId);
            }

            glyphIds.Add(instanceId);
        }

        _instances[nodeId] = new TextInstanceData(
            glyphIds, groupKey, layout, font, content, fontSize, fontWeight);

        logger.LogDebug("Registered text rendering for node {NodeId} with {GlyphCount} glyphs", nodeId, glyphIds.Count);

        return Result.Success();
    }

    public DeletionResult DeregisterText(Id<GuiNode> nodeId)
    {
        if (!_instances.Remove(nodeId, out var data))
        {
            return DeletionResult.NotFound();
        }

        var groupId = _textGroups[data.GroupKey];
        foreach (var glyphId in data.GlyphInstanceIds)
        {
            renderingInstanceManager.Remove(groupId, glyphId);
        }

        logger.LogDebug("Deregistered text rendering for node {NodeId}", nodeId);

        return DeletionResult.Success();
    }

    public Result UpdateText(Id<GuiNode> nodeId, string newContent, float? fontSize = null, float? fontWeight = null)
    {
        if (!_instances.TryGetValue(nodeId, out var data))
        {
            return Result.Success();
        }

        var effectiveFontSize = fontSize ?? data.CachedFontSize;
        var effectiveFontWeight = fontWeight ?? data.CachedFontWeight;

        if (data.CachedContent == newContent
            && float.Abs(effectiveFontSize - data.CachedFontSize) < 0.001f
            && float.Abs(effectiveFontWeight - data.CachedFontWeight) < 0.001f)
        {
            return Result.Success();
        }

        var newLayout = TextLayoutEngine.ComputeLayout(newContent, data.Font, effectiveFontSize);
        var oldGlyphIds = data.GlyphInstanceIds;

        // If font weight changed, we may need a different group
        var depth = guiDepthService.GetDepth(nodeId);
        var newGroupKey = data.GroupKey with { FontWeight = effectiveFontWeight, Depth = depth };
        var groupChanged = !newGroupKey.Equals(data.GroupKey);

        if (groupChanged)
        {
            // Font weight changed — deregister all old glyphs and re-register in new group
            var oldGroupId = _textGroups[data.GroupKey];
            foreach (var glyphId in oldGlyphIds)
                renderingInstanceManager.Remove(oldGroupId, glyphId);

            // Re-register all glyphs in the new group
            // Extract font atlas from the group key
            if (!newGroupKey.FontAtlasId.TryGetAsTypedId<RGB>(out var fontAtlasId))
            {
                return new ResultProblem("Failed to get typed font atlas ID");
            }

            var newGroupId = GetOrCreateGroup(newGroupKey, fontAtlasId, effectiveFontWeight);
            var newGlyphIds = new List<Id<Shaders.MsdfText.Instance>>();

            foreach (var glyph in newLayout)
            {
                var glyphInstance = new Shaders.MsdfText.Instance(
                    iPosPx: glyph.PositionPx,
                    iSizePx: glyph.SizePx,
                    iTint: Vector4D<float>.One,
                    iUvMin: glyph.UvMin,
                    iUvMax: glyph.UvMax);

                if (renderingInstanceManager.Add(newGroupId, glyphInstance)
                    .TryPickProblems(out var problems, out var instanceId))
                {
                    return problems.Prepend("Failed to register glyph for text: nodeId={0}", nodeId);
                }

                newGlyphIds.Add(instanceId);
            }

            _instances[nodeId] = data with
            {
                GlyphInstanceIds = newGlyphIds,
                GroupKey = newGroupKey,
                CachedLayout = newLayout,
                CachedContent = newContent,
                CachedFontSize = effectiveFontSize,
                CachedFontWeight = effectiveFontWeight,
            };

            return Result.Success();
        }

        var currentGroupId = _textGroups[data.GroupKey];
        var updatedGlyphIds = new List<Id<Shaders.MsdfText.Instance>>(oldGlyphIds);

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

            if (renderingInstanceManager.Update(currentGroupId, glyphId, glyphInstance)
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

            if (renderingInstanceManager.Add(currentGroupId, glyphInstance)
                .TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to register new glyph for text: nodeId={0}", nodeId);
            }

            updatedGlyphIds.Add(instanceId);
        }

        // Deregister excess glyphs if text got shorter
        for (var i = newLayout.Count; i < oldGlyphIds.Count; i++)
        {
            renderingInstanceManager.Remove(currentGroupId, oldGlyphIds[i]);
        }

        if (newLayout.Count < oldGlyphIds.Count)
        {
            updatedGlyphIds.RemoveRange(newLayout.Count, oldGlyphIds.Count - newLayout.Count);
        }

        _instances[nodeId] = data with
        {
            GlyphInstanceIds = updatedGlyphIds,
            CachedLayout = newLayout,
            CachedContent = newContent,
            CachedFontSize = effectiveFontSize,
            CachedFontWeight = effectiveFontWeight
        };

        return Result.Success();
    }

    private GroupId<Shaders.MsdfText.Instance> GetOrCreateGroup(
        TextGroupKey key,
        TextureId<RGB> fontAtlasId,
        float fontWeight)
    {
        if (_textGroups.TryGetValue(key, out var existingGroupId))
        {
            return existingGroupId;
        }

        var groupParameters = new Shaders.MsdfText.EntityParameters(
            UFontAtlas: fontAtlasId,
            UFontWeight: fontWeight);

        if (renderingGroupManager.Register<Shaders.MsdfText.Vertex, Shaders.MsdfText.Instance, IDefaultFrameFormat>(
                _quadGeometryId, _shader, sharedRenderingService.GuiPass, GuiRenderState,
                sortKey: GuiSortKey + key.Depth, groupParameters: groupParameters)
            .TryPickProblems(out _, out var groupId))
        {
            throw new InvalidOperationException(
                $"Failed to register GUI text group for font atlas {key.FontAtlasId}");
        }

        _textGroups[key] = groupId;
        return groupId;
    }

    private bool TryUpdateTextGlyphs(Id<GuiNode> nodeId, TextInstanceData instanceData)
    {
        // Hidden nodes should produce zeroed instances without requiring a layout position,
        // so they aren't deregistered from the rendering system.
        if (guiNodeStateService.TryGetState(nodeId, out var state) && !state.HasFlag(GuiNodeState.Show))
        {
            return TryZeroOutGlyphs(instanceData);
        }

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

        var scale = instanceData.CachedFontSize / instanceData.Font.Metrics.EmSize;
        var baselineOffset = instanceData.Font.Metrics.Ascender * scale;

        var groupId = _textGroups[instanceData.GroupKey];

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

            if (renderingInstanceManager.Update(groupId, glyphId, glyphInstance)
                .TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
                return false;
            }
        }

        return true;
    }

    private bool TryZeroOutGlyphs(TextInstanceData instanceData)
    {
        var groupId = _textGroups[instanceData.GroupKey];

        foreach (var glyphId in instanceData.GlyphInstanceIds)
        {
            if (renderingInstanceManager.Update(groupId, glyphId, default)
                .TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
                return false;
            }
        }

        return true;
    }
}
