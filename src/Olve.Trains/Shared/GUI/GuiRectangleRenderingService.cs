using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;

namespace Olve.Trains.Shared.GUI;

public class GuiRectangleRenderingService(
    ILogger<GuiRectangleRenderingService> logger,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    Provider<LayoutContext> layoutContext,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService,
    GuiDepthService guiDepthService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([guiLayoutService]);

    private static readonly RenderState GuiRenderState = new(BlendMode.Alpha, DepthWrite: false, DepthTest: false);
    private const int GuiSortKey = 1000;

    private readonly record struct RectGroupKey(UntypedTextureId TextureId, int Depth);

    private readonly record struct InstanceData(
        Id<Shaders.TexturedRectangle.Instance> InstanceId,
        RectGroupKey GroupKey);

    private readonly Dictionary<Id<GuiNode>, InstanceData> _instances = new();
    private readonly Dictionary<RectGroupKey, GroupId<Shaders.TexturedRectangle.Instance>> _textureGroups = new();
    private readonly List<Id<GuiNode>> _nodesToDelete = [];
    private readonly List<ResultProblem> _updateProblems = [];
    private readonly Shaders.TexturedRectangle _shader = new()
    {
        BlendState = RenderState.AlphaBlendNoDepthWrite,
    };

    private GeometryId<Shaders.TexturedRectangle.Vertex> _quadGeometryId = null!;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load textured rectangle shader");
        }

        var quadVertices = new Shaders.TexturedRectangle.Vertex[UnitQuad.VertexCount];
        UnitQuad.Populate(quadVertices);

        if (geometryManager.Register<Shaders.TexturedRectangle.Vertex>(quadVertices, ReadOnlySpan<uint>.Empty)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register quad geometry");
        }

        _quadGeometryId = geometryId;

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var (nodeId, _) in _instances)
        {
            if (DeregisterTexturedRectangle(nodeId).TryPickProblems(out var problems))
            {
                logger.Log(problems);
            }
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        var designSize = layoutContext.Value.ToPx(layoutContext.Value.DesignSize);
        _shader.UResolution = new Vector2D<float>(designSize.X.Value, designSize.Y.Value);

        _nodesToDelete.Clear();
        _updateProblems.Clear();

        foreach (var (nodeId, instanceData) in _instances)
        {
            if (!TryBuildInstanceData(nodeId, out var rectInstance))
            {
                _nodesToDelete.Add(nodeId);
                continue;
            }

            var groupId = _textureGroups[instanceData.GroupKey];

            if (renderingInstanceManager.Update(groupId, instanceData.InstanceId, rectInstance)
                .TryPickProblems(out var problems))
            {
                _nodesToDelete.Add(nodeId);
                _updateProblems.AddRange(problems);
            }
        }

        foreach (var nodeId in _nodesToDelete)
        {
            if (DeregisterTexturedRectangle(nodeId).TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
            }
        }

        if (_updateProblems.Any())
        {
            return Result.Failure(_updateProblems.Prepend(
                new ResultProblem("Failed while updating GUI rectangle elements")));
        }

        return Result.Success();
    }

    public Result RegisterTexturedRectangle(Id<GuiNode> nodeId, TextureId<RGBA> textureId)
    {
        if (_instances.Remove(nodeId, out var oldInstance))
        {
            var oldGroupId = _textureGroups[oldInstance.GroupKey];
            if (renderingInstanceManager.Remove(oldGroupId, oldInstance.InstanceId)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        UntypedTextureId untypedTextureId = textureId;
        var depth = guiDepthService.GetDepth(nodeId);
        var groupKey = new RectGroupKey(untypedTextureId, depth);
        var groupId = GetOrCreateGroup(groupKey, textureId);

        var initialInstance = new Shaders.TexturedRectangle.Instance(
            iPosPx: Vector2D<float>.Zero,
            iSizePx: Vector2D<float>.One,
            iTint: Vector4D<float>.One,
            iBorderWidthPx: Vector4D<float>.Zero,
            iBorderColor: Vector4D<float>.Zero,
            iBorderRadiusPx: Vector4D<float>.Zero);

        if (renderingInstanceManager.Add(groupId, initialInstance)
            .TryPickProblems(out var addProblems, out var instanceId))
        {
            return addProblems.Prepend("Failed to register rectangle: nodeId={0}, textureId={1}", nodeId, textureId);
        }

        _instances[nodeId] = new InstanceData(instanceId, groupKey);

        logger.LogDebug("Registered rectangle rendering for node {NodeId}", nodeId);

        return Result.Success();
    }

    public DeletionResult DeregisterTexturedRectangle(Id<GuiNode> nodeId)
    {
        if (!_instances.Remove(nodeId, out var instanceData))
        {
            return DeletionResult.NotFound();
        }

        var groupId = _textureGroups[instanceData.GroupKey];

        if (renderingInstanceManager.Remove(groupId, instanceData.InstanceId)
            .TryPickProblems(out var problems))
        {
            return DeletionResult.Error(problems);
        }

        logger.LogDebug("Deregistered rectangle rendering for node {NodeId}", nodeId);

        return DeletionResult.Success();
    }

    private GroupId<Shaders.TexturedRectangle.Instance> GetOrCreateGroup(
        RectGroupKey key,
        TextureId<RGBA> typedTextureId)
    {
        if (_textureGroups.TryGetValue(key, out var existingGroupId))
        {
            return existingGroupId;
        }

        var groupParameters = new Shaders.TexturedRectangle.EntityParameters(UTexture: typedTextureId);

        if (renderingGroupManager.Register<Shaders.TexturedRectangle.Vertex, Shaders.TexturedRectangle.Instance>(
                _quadGeometryId, _shader, GuiRenderState,
                sortKey: GuiSortKey + key.Depth, groupParameters: groupParameters)
            .TryPickProblems(out _, out var groupId))
        {
            throw new InvalidOperationException(
                $"Failed to register GUI rectangle group for texture {key.TextureId}");
        }

        _textureGroups[key] = groupId;
        return groupId;
    }

    private bool TryBuildInstanceData(
        Id<GuiNode> nodeId,
        out Shaders.TexturedRectangle.Instance instance)
    {
        instance = default;

        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            element is not IRenderableAsRectangle renderableAsRectangle)
        {
            return false;
        }

        var rectData = renderableAsRectangle.TexturedRectangleData;

        var border = rectData.Border ?? Border.None;
        var borderWidthPx = new Vector4D<float>(
            layoutContext.Value.ToPx(border.Width.Left).Value,
            layoutContext.Value.ToPx(border.Width.Top).Value,
            layoutContext.Value.ToPx(border.Width.Right).Value,
            layoutContext.Value.ToPx(border.Width.Bottom).Value
        );

        var borderRadiusPx = new Vector4D<float>(
            layoutContext.Value.ToPx(border.Radius.TopLeft).Value,
            layoutContext.Value.ToPx(border.Radius.TopRight).Value,
            layoutContext.Value.ToPx(border.Radius.BottomRight).Value,
            layoutContext.Value.ToPx(border.Radius.BottomLeft).Value
        );

        instance = new Shaders.TexturedRectangle.Instance(
            iPosPx: new Vector2D<float>(boxPosition.Position.X.Value, boxPosition.Position.Y.Value),
            iSizePx: new Vector2D<float>(boxPosition.Size.X.Value, boxPosition.Size.Y.Value),
            iTint: rectData.Color.ToVector(),
            iBorderWidthPx: borderWidthPx,
            iBorderColor: border.Color.ToVector(),
            iBorderRadiusPx: borderRadiusPx);

        return true;
    }
}
