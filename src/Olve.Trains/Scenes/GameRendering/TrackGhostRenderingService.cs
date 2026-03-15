using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Shared.Rendering;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackGhostRenderingService(
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    TerrainRenderingService terrainRenderingService,
    SharedRenderingService sharedRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private readonly record struct GhostEntry(
        GeometryId<Shaders.LineStrip.Vertex> GeometryId,
        GroupId<Shaders.LineStrip.Instance> GroupId);

    private readonly Dictionary<Id<Track>, GhostEntry> _entries = new();
    private readonly Shaders.LineStrip _shader = new()
    {
        UOpacity = 1.0f,
        UColorMix = 0.0f,
        BlendState = RenderState.AlphaBlendNoDepth,
    };

    private static readonly Shaders.LineStrip.Instance IdentityInstance = new(Matrix4X4<float>.Identity);

    public Result Load()
    {
        return renderingServiceHelper.LoadShader(_shader);
    }

    public Result Register(
        Id<Track> trackId,
        LineStripData data,
        Shaders.LineStrip.EntityParameters? groupParameters = null)
    {
        if (data.Validate().TryPickProblems(out var problems))
        {
            return problems.Prepend("Invalid line strip data");
        }

        var vertices = new Shaders.LineStrip.Vertex[data.VertexCount];
        data.Populate(vertices);

        if (geometryManager.Register<Shaders.LineStrip.Vertex>(
                vertices, ReadOnlySpan<uint>.Empty, PrimitiveType.LineStrip, BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register ghost geometry");
        }

        if (renderingGroupManager.Register<Shaders.LineStrip.Vertex, Shaders.LineStrip.Instance, IDefaultFrameFormat>(
                geometryId, _shader, sharedRenderingService.MainPass, RenderState.AlphaBlendNoDepth,
                PrimitiveType.LineStrip, groupParameters: groupParameters)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register ghost group");
        }

        if (renderingInstanceManager.Add(groupId, IdentityInstance)
            .TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to add ghost instance");
        }

        _entries[trackId] = new GhostEntry(geometryId, groupId);

        return Result.Success();
    }

    public Result Update(
        Id<Track> trackId,
        LineStripData? data = null,
        Shaders.LineStrip.EntityParameters? groupParameters = null)
    {
        if (!_entries.TryGetValue(trackId, out var entry))
        {
            return new ResultProblem("Ghost track '{0}' is not registered", trackId);
        }

        if (data != null)
        {
            if (data.Validate().TryPickProblems(out var problems))
            {
                return problems.Prepend("Invalid line strip data");
            }

            var vertices = new Shaders.LineStrip.Vertex[data.VertexCount];
            data.Populate(vertices);

            if (geometryManager.UpdateVertices(entry.GeometryId, vertices)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to update ghost vertices");
            }
        }

        if (groupParameters is not null)
        {
            if (renderingGroupManager.SetGroupParameters(entry.GroupId, groupParameters)
                .TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to update ghost group parameters");
            }
        }

        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_entries.Remove(trackId, out var entry))
        {
            return Result.Success();
        }

        return Result.Concat(
            renderingGroupManager.Deregister(entry.GroupId),
            geometryManager.Deregister(entry.GeometryId));
    }

    public Result Update()
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        return Result.Success();
    }

    public Result Unload()
    {
        foreach (var entry in _entries.Values)
        {
            renderingGroupManager.Deregister(entry.GroupId);
            geometryManager.Deregister(entry.GeometryId);
        }
        _entries.Clear();

        return Result.Success();
    }
}
