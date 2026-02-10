using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Shaders;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiRectangleRenderingService(
    ILoggingManager loggingManager,
    RenderingManager2D renderingManager2D,
    ShaderEntityManager shaderEntityManager,
    Provider<LayoutContext> layoutContext,
    GuiDepthService guiDepthService,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([guiLayoutService]);

    private readonly record struct InstanceData(RenderingInstanceId InstanceId, UntypedTextureId TextureId);

    private readonly Dictionary<Id<GuiNode>, InstanceData> _instances = new();
    private readonly List<Id<GuiNode>> _nodesToDelete = [];
    private readonly List<ResultProblem> _updateProblems = [];
    private readonly Shaders.TexturedRectangle _shader = new()
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
            if (!TryBuildInstanceData(nodeId, out var rectInstance, out var depth))
            {
                _nodesToDelete.Add(nodeId);
                continue;
            }

            if (!instanceData.TextureId.TryGetAsTypedId<RGBA>(out var textureId))
            {
                // TODO: Improve this error message
                return new ResultProblem("Failed to find texture id for node '{0}'", nodeId);
            }

            var entityParams = new Shaders.TexturedRectangle.EntityParameters(UTexture: textureId);

            if (renderingManager2D.Update(instanceData.InstanceId, rectInstance, depth, entityParams)
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

    protected override Result OnRender(TimeSpan deltaTime)
    {
        if (_instances.Count == 0)
        {
            return Result.Success();
        }

        return renderingManager2D.Render(_shader);
    }

    public Result RegisterTexturedRectangle(Id<GuiNode> nodeId, TextureId<RGBA> textureId)
    {
        ResultProblemCollection? problems;
        if (_instances.Remove(nodeId, out var oldInstance))
        {
            if (renderingManager2D
                .Deregister(oldInstance.InstanceId)
                .TryPickProblems(out problems))
            {
                return problems;
            }
        }

        var initialInstance = new Shaders.TexturedRectangle.Instance(
            iPosPx: Vector2D<float>.Zero,
            iSizePx: Vector2D<float>.One,
            iTint: Vector4D<float>.One,
            iBorderWidthPx: Vector4D<float>.Zero,
            iBorderColor: Vector4D<float>.Zero,
            iBorderRadiusPx: Vector4D<float>.Zero);

        var entityParams = new Shaders.TexturedRectangle.EntityParameters(UTexture: textureId);

        if (renderingManager2D.Register(_shader.RenderingId, initialInstance, 0f, entityParams)
            .TryPickProblems(out problems, out var renderingInstanceId))
        {
            return problems.Prepend("Failed to register rectangle: nodeId={0}, textureId={1}", nodeId, textureId);
        }

        _instances[nodeId] = new InstanceData(renderingInstanceId, textureId);

        LoggingManager.Log(LogLevel.Debug, $"Registered rectangle rendering for node {nodeId}");

        return Result.Success();
    }

    public DeletionResult DeregisterTexturedRectangle(Id<GuiNode> nodeId)
    {
        if (!_instances.Remove(nodeId, out var instanceData))
        {
            return DeletionResult.NotFound();
        }

        if (renderingManager2D
            .Deregister(instanceData.InstanceId)
            .TryPickProblems(out var problems))
        {
            return DeletionResult.Error(problems);
        }

        LoggingManager.Log(LogLevel.Debug, $"Deregistered rectangle rendering for node {nodeId}");

        return DeletionResult.Success();
    }

    private bool TryBuildInstanceData(
        Id<GuiNode> nodeId,
        out Shaders.TexturedRectangle.Instance instance,
        out float depth)
    {
        instance = default;
        depth = 0;

        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            element is not IRenderableAsRectangle renderableAsRectangle)
        {
            return false;
        }

        depth = -guiDepthService.GetDepth(nodeId);

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
