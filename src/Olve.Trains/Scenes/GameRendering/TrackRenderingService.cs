using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackRenderingService(
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    SceneLightService sceneLightService,
    ShadowMapService shadowMapService,
    TrackService trackService,
    TerrainRenderingService terrainRenderingService,
    SharedRenderingService sharedRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService, shadowMapService]);

    private readonly Shaders.Track _shader = new()
    {
        BlendState = RenderState.Opaque,
        UColor = new Vector3D<float>(0.85f, 0.85f, 0.85f),
    };

    private readonly Dictionary<Id<Track>, Id<Shaders.Track.Instance>> _trackInstanceIds = new();
    private readonly Dictionary<Id<Track>, Id<Shaders.Track.Instance>> _shadowInstanceIds = new();
    private GroupId<Shaders.Track.Instance> _groupId = null!;
    private ShadowMapService.ShadowGroupHandle<Shaders.Track.Instance> _shadowGroupId;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track shader");
        }

        // Generate template mesh
        var (vertices, indices) = TrackTemplateMeshService.Generate<Shaders.Track.Vertex>();

        // Register geometry
        if (geometryManager.Register(vertices, indices).TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register track geometry");
        }

        // Register main group
        if (renderingGroupManager.Register<Shaders.Track.Vertex, Shaders.Track.Instance, IDefaultFrameFormat>(
                geometryId, _shader, sharedRenderingService.MainPass, _shader.BlendState)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register track group");
        }

        _groupId = groupId;

        // Register shadow group
        if (shadowMapService.RegisterTrackShadowGroup(geometryId)
            .TryPickProblems(out problems, out var shadowGroup))
        {
            return problems.Prepend("Failed to register track shadow group");
        }

        _shadowGroupId = shadowGroup;

        return Result.Success();
    }

    public Result Register(Id<Track> trackId)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        var instance = CreateInstance(track.Start, track.End);

        if (renderingInstanceManager.Add(_groupId, instance)
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add track instance for '{0}'", trackId);
        }

        _trackInstanceIds[trackId] = instanceId;

        // Mirror to shadow group
        if (shadowMapService.AddInstance(_shadowGroupId, instance)
            .TryPickProblems(out problems, out var shadowInstanceId))
        {
            return problems.Prepend("Failed to add track shadow instance for '{0}'", trackId);
        }

        _shadowInstanceIds[trackId] = shadowInstanceId;

        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (_trackInstanceIds.Remove(trackId, out var instanceId))
        {
            if (renderingInstanceManager.Remove(_groupId, instanceId).TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        if (_shadowInstanceIds.Remove(trackId, out var shadowInstanceId))
        {
            if (shadowMapService.RemoveInstance(_shadowGroupId, shadowInstanceId).TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);
        shadowMapService.ApplyShaderParameters(_shader);

        return Result.Success();
    }

    private static Shaders.Track.Instance CreateInstance(TrackEndpoint start, TrackEndpoint end)
    {
        var tangentScale = (start.Point - end.Point).Length;
        var startTangent = -start.Tangent * tangentScale;
        var endTangent = end.Tangent * tangentScale;

        return new Shaders.Track.Instance(
            iP0: start.Point,
            iP1: end.Point,
            iT0: startTangent,
            iT1: endTangent);
    }
}
