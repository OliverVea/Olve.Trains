using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class GuiRectangleRenderingService(ILoggingManager loggingManager,
    RenderingManager2D renderingManager2D,
    ShaderEntityManager shaderEntityManager,
    Provider<LayoutContext> layoutContext,
    GuiDepthService guiDepthService,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([ guiLayoutService ]);

    private readonly Dictionary<Id<GuiNode>, RenderingInstanceId> _renderingInstanceIds = new();
    private readonly List<Id<GuiNode>> _nodesToDelete = [];
    private readonly List<ResultProblem> _updateProblems = [];
    private readonly Shaders.Rectangle _shader = new()
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
        _shader.UResolution = layoutContext.Value.ViewportSize.As<float>();

        _nodesToDelete.Clear();
        _updateProblems.Clear();

        foreach (var (nodeId, instanceId) in _renderingInstanceIds)
        {
            if (!TryBuildRectangleData(nodeId, out var rectangleData))
            {
                _nodesToDelete.Add(nodeId);
                continue;
            }

            if (renderingManager2D.UpdateRectangle(instanceId, rectangleData).TryPickProblems(out var problems))
            {
                _nodesToDelete.Add(nodeId);
                _updateProblems.AddRange(problems);
            }
        }

        foreach (var nodeId in _nodesToDelete)
        {
            if (DeregisterRectangle(nodeId)
                .TryPickProblems(out var problems))
            {
                _updateProblems.AddRange(problems);
            }
        }

        if (_updateProblems.Any())
        {
            return Result.Failure(_updateProblems.Prepend(new ResultProblem("Failed while updating GUI rectangle elements")));
        }

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        return renderingManager2D.Render(_shader);
    }

    public Result RegisterRectangle(Id<GuiNode> nodeId)
    {
        ResultProblemCollection? problems;
        if (_renderingInstanceIds.Remove(nodeId, out var oldInstanceId))
        {
            if (renderingManager2D
                .DeregisterRectangle(oldInstanceId)
                .TryPickProblems(out problems))
            {
                return problems;
            }
        }

        if (renderingManager2D.RegisterRectangle(_shader.RenderingId, RectangleData.Default)
            .TryPickProblems(out problems, out var renderingInstanceId))
        {
            return problems;
        }

        _renderingInstanceIds[nodeId] = renderingInstanceId;

        LoggingManager.Log(LogLevel.Debug, $"Registered rectangle rendering for node {nodeId}");

        return Result.Success();
    }

    public DeletionResult DeregisterRectangle(Id<GuiNode> nodeId)
    {
        if (!_renderingInstanceIds.Remove(nodeId, out var instanceId))
        {
            return DeletionResult.NotFound();
        }

        if (renderingManager2D
            .DeregisterRectangle(instanceId)
            .TryPickProblems(out var problems))
        {
            return DeletionResult.Error(problems);
        }

        LoggingManager.Log(LogLevel.Debug, $"Deregistered rectangle rendering for node {nodeId}");

        return DeletionResult.Success();
    }

    private bool TryBuildRectangleData(Id<GuiNode> nodeId, out RectangleData rectangleData)
    {
        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            !guiDepthService.GetDepth(nodeId).TryPickValue(out var depth) ||
            element is not IRenderableAsRectangle renderableAsRectangle)
        {
            rectangleData = RectangleData.Default;
            return false;
        }

        rectangleData = new RectangleData
        {
            PositionPx = new Vector2D<float>(boxPosition.Position.X.Value, boxPosition.Position.Y.Value),
            SizePx = new Vector2D<float>(boxPosition.Size.X.Value, boxPosition.Size.Y.Value),
            ColorRgba = renderableAsRectangle.RectangleData.Color,
            Depth = -depth
        };

        return true;
    }
}