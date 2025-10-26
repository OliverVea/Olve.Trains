using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
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

public class GuiTexturedRectangleRenderingService(
    ILoggingManager loggingManager,
    RenderingManager2D renderingManager2D,
    ShaderEntityManager shaderEntityManager,
    TextureEntityManager textureEntityManager,
    Provider<LayoutContext> layoutContext,
    GuiDepthService guiDepthService,
    TextureLoadingService textureLoadingService,
    GuiElementService guiElementService,
    GuiLayoutService guiLayoutService) : SceneService(loggingManager)
{
    public override int Priority => GetPriorityFromDependencies([guiLayoutService]);

    private readonly Dictionary<Id<GuiNode>, RenderingInstanceId> _renderingInstanceIds = new();
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
        _shader.UResolution = layoutContext.Value.ViewportSize.As<float>();

        _nodesToDelete.Clear();
        _updateProblems.Clear();

        foreach (var (nodeId, instanceId) in _renderingInstanceIds)
        {
            if (!TryBuildTexturedRectangleData(nodeId, out var texturedRectangleData))
            {
                _nodesToDelete.Add(nodeId);
                continue;
            }

            if (renderingManager2D.UpdateRectangle(instanceId, texturedRectangleData)
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
                new ResultProblem("Failed while updating GUI textured rectangle elements")));
        }

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        if (_renderingInstanceIds.Count == 0)
        {
            return Result.Success();
        }

        return renderingManager2D.Render(_shader);
    }

    public Result RegisterTexturedRectangle(Id<GuiNode> nodeId, RenderingId<TextureData> renderingId)
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

        var initialData = new RectangleData
        {
            PositionPx = Vector2D<float>.Zero,
            SizePx = Vector2D<float>.One,
            TintRgba = Vector4D<float>.One,
            TextureId = renderingId,
            Depth = 0
        };

        if (renderingManager2D.RegisterRectangle(_shader.RenderingId, initialData)
            .TryPickProblems(out problems, out var renderingInstanceId))
        {
            return problems.Prepend("Failed to register rectangle: nodeId={0}, textureId={1}", nodeId, renderingInstanceId);
        }

        _renderingInstanceIds[nodeId] = renderingInstanceId;

        LoggingManager.Log(LogLevel.Debug, $"Registered textured rectangle rendering for node {nodeId}");

        return Result.Success();
    }

    public DeletionResult DeregisterTexturedRectangle(Id<GuiNode> nodeId)
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

        LoggingManager.Log(LogLevel.Debug, $"Deregistered textured rectangle rendering for node {nodeId}");

        return DeletionResult.Success();
    }

    private bool TryBuildTexturedRectangleData(Id<GuiNode> nodeId, [MaybeNullWhen(false)] out RectangleData rectangleData)
    {
        if (!guiLayoutService.TryGetBoxPosition(nodeId, out var boxPosition) ||
            !guiElementService.TryGetElement(nodeId, out var element) ||
            !guiDepthService.GetDepth(nodeId).TryPickValue(out var depth) ||
            element is not IRenderableAsTexturedRectangle renderableAsTexturedRectangle ||
            textureLoadingService.LoadTextureOrFallbackIfNull(renderableAsTexturedRectangle.TexturedRectangleData.TexturePath)
                .TryPickProblems(out var problems, out var textureId))
        {
            rectangleData = null;
            return false;
        }

        // Retrieve texture handle for shader
        if (textureEntityManager.GetRegistration(textureId)
            .TryPickProblems(out problems, out var textureReg))
        {
            LoggingManager.Log(LogLevel.Error,
                $"Failed to get texture registration for node {nodeId}: {Result.Failure(problems)}");
            rectangleData = null;
            return false;
        }

        // Set texture uniform on shader
        _shader.UTexture = textureReg.Texture;

        rectangleData = new RectangleData
        {
            PositionPx = new Vector2D<float>(boxPosition.Position.X.Value, boxPosition.Position.Y.Value),
            SizePx = new Vector2D<float>(boxPosition.Size.X.Value, boxPosition.Size.Y.Value),
            TintRgba = renderableAsTexturedRectangle.TexturedRectangleData.Color,
            TextureId = textureId,
            Depth = -depth
        };

        return true;
    }
}
