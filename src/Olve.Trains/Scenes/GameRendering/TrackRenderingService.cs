using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Light;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackRenderingService(
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
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

    private readonly Dictionary<Id<Track>, Shaders.Track.Instance> _trackInstances = new();
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;
    private bool _dirty;

    public Result Load()
    {
        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track shader");
        }

        // Generate template mesh
        var (vertices, indices) = TrackTemplateMeshService.Generate();

        // Marshal vertex data
        var vertexFloats = new float[vertices.Length * TrackTemplateMeshService.TrackVertex.FloatCount];
        var span = vertexFloats.AsSpan();
        var offset = 0;
        foreach (var vertex in vertices)
        {
            vertex.WriteTo(span.Slice(offset, TrackTemplateMeshService.TrackVertex.FloatCount));
            offset += TrackTemplateMeshService.TrackVertex.FloatCount;
        }

        // Create instanced registration with empty instance data initially
        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                (uint)vertices.Length,
                indices,
                TrackTemplateMeshService.TrackVertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Track.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create track instanced buffers");
        }

        _registration = registration;

        return Result.Success();
    }

    public Result Register(Id<Track> trackId)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        var instance = CreateInstance(track.Start, track.End);
        _trackInstances[trackId] = instance;
        _dirty = true;

        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (_trackInstances.Remove(trackId))
        {
            _dirty = true;
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        if (_dirty)
        {
            RebuildInstanceBuffer();
            _dirty = false;
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        return renderingManager3D.RenderInstanced(
            _shader,
            _registration,
            (uint)_trackInstances.Count);
    }

    public Result Unload()
    {
        instancedBufferManager.DeleteMeshInstanceBuffers(_registration);
        return renderingServiceHelper.UnloadShader(_shader);
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

    private void RebuildInstanceBuffer()
    {
        var instanceCount = _trackInstances.Count;
        if (instanceCount == 0)
        {
            instancedBufferManager.UpdateMeshInstanceBuffer(
                _registration,
                ReadOnlySpan<float>.Empty,
                0,
                BufferUsageARB.DynamicDraw);
            return;
        }

        var floats = new float[instanceCount * Shaders.Track.Instance.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var instance in _trackInstances.Values)
        {
            instance.WriteTo(span.Slice(offset, Shaders.Track.Instance.FloatCount));
            offset += Shaders.Track.Instance.FloatCount;
        }

        instancedBufferManager.UpdateMeshInstanceBuffer(
            _registration,
            floats,
            (uint)instanceCount,
            BufferUsageARB.DynamicDraw);
    }
}
