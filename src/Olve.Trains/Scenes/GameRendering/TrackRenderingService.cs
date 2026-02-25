using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackRenderingService(
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    CameraSceneService cameraSceneService,
    RenderingServiceHelper renderingServiceHelper,
    SceneLightService sceneLightService,
    TrackService trackService,
    TerrainRenderingService terrainRenderingService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private readonly Shaders.Track _shader = new()
    {
        BlendState = RenderState.Opaque,
        UColor = new Vector3D<float>(0.85f, 0.85f, 0.85f),
    };

    private readonly Dictionary<Id<Track>, Id<Shaders.Track.Instance>> _trackInstanceIds = new();
    private GroupId<Shaders.Track.Instance> _groupId = null!;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track shader");
        }

        // Generate template mesh
        var (vertices, indices) = TrackTemplateMeshService.Generate<Shaders.Track.Vertex>();

        // Register geometry
        if (geometryManager.Register<Shaders.Track.Vertex>(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register track geometry");
        }

        // Register group
        if (renderingGroupManager.Register<Shaders.Track.Vertex, Shaders.Track.Instance>(
                geometryId, _shader, _shader.BlendState)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register track group");
        }

        _groupId = groupId;

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

        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_trackInstanceIds.Remove(trackId, out var instanceId))
        {
            return Result.Success();
        }

        return renderingInstanceManager.Remove(_groupId, instanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

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
