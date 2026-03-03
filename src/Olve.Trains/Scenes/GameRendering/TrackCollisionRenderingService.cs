using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameRendering;

public class TrackCollisionRenderingService(
    MeshRenderingService meshRenderingService,
    EventQueueFactory eventQueueFactory,
    TrackService trackService,
    TrackCollisionService trackCollisionService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    private readonly Dictionary<Id<Track>, MeshRenderingService.MeshInstanceHandle[]> _instanceHandles = new();

    private readonly EventQueue<Id<Track>> _toAddQueue = eventQueueFactory.Create(trackService.OnTrackAdded);
    private readonly EventQueue<Id<Track>> _toRemoveQueue = eventQueueFactory.Create(trackService.OnTrackRemoved);

    private MeshRenderingService.MeshGroupHandle _groupHandle;

    public Result Load()
    {
        Span<Vector3D<float>> positions = stackalloc Vector3D<float>[UnitCube.VertexCount];
        Span<Vector3D<float>> normals = stackalloc Vector3D<float>[UnitCube.VertexCount];
        UnitCube.GetVertices(positions, normals);

        // Center the cube: (0,0,0)→(1,1,1) to (-0.5,-0.5,-0.5)→(0.5,0.5,0.5)
        var offset = new Vector3D<float>(0.5f, 0.5f, 0.5f);
        var vertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Shaders.Default.Vertex(positions[i] - offset, normals[i], default);
        }

        var indices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        if (meshRenderingService.RegisterMeshGroup(
                vertices, indices,
                renderState: RenderState.AlphaBlend,
                parameters: new Shaders.Default.EntityParameters(
                    UColor: new Vector3D<float>(1f, 1f, 0f),
                    UOpacity: 0.3f,
                    UColorOverride: new Vector3D<float>(1f, 1f, 0f),
                    UColorMix: 1f))
            .TryPickProblems(out var problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register track collision debug mesh group");
        }

        _groupHandle = groupHandle;

        _toAddQueue.SetHandler(AddTrack).Init();
        _toRemoveQueue.SetHandler(RemoveTrack).Init();

        return Result.Success();
    }

    public Result Unload()
    {
        _toAddQueue.Cleanup();
        _toRemoveQueue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        _toAddQueue.Update();
        _toRemoveQueue.Update();
        return Result.Success();
    }

    private Result AddTrack(Id<Track> trackId)
    {
        if (_instanceHandles.ContainsKey(trackId))
        {
            return new ResultProblem("Track collision debug instances already exist for track '{0}'", trackId);
        }

        if (!trackCollisionService.TryGetMatrices(trackId, out var matrices))
        {
            return new ResultProblem("No collision matrices found for track '{0}'", trackId);
        }

        var handles = new MeshRenderingService.MeshInstanceHandle[matrices.Length];

        for (var i = 0; i < matrices.Length; i++)
        {
            if (meshRenderingService.AddInstance(_groupHandle, matrices[i])
                .TryPickProblems(out var problems, out var handle))
            {
                return problems.Prepend("Failed to add debug instance for track '{0}' segment {1}", trackId, i);
            }

            handles[i] = handle;
        }

        _instanceHandles[trackId] = handles;
        return Result.Success();
    }

    private Result RemoveTrack(Id<Track> trackId)
    {
        if (!_instanceHandles.Remove(trackId, out var handles))
        {
            return Result.Success();
        }

        foreach (var handle in handles)
        {
            meshRenderingService.RemoveInstance(handle);
        }

        return Result.Success();
    }
}
