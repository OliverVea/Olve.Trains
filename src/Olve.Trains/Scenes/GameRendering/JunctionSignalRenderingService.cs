using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Junctions;

namespace Olve.Trains.Scenes.GameRendering;

public class JunctionSignalRenderingService(
    EventQueueFactory eventQueueFactory,
    MeshRenderingService meshRenderingService,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    GridService gridService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([meshRenderingService]);

    private readonly Dictionary<Id<Junction>, MeshRenderingService.MeshInstanceHandle> _instanceIds = new();

    private readonly EventQueue<Id<Junction>> _junctionSignalAddedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionAdded);
    private readonly EventQueue<Id<Junction>> _junctionSignalRemovedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionRemoved);

    private MeshRenderingService.MeshGroupHandle _groupHandle;

    public Result Load()
    {
        _junctionSignalAddedQueue
            .SetHandler(OnAdded)
            .Init();
        _junctionSignalRemovedQueue
            .SetHandler(OnRemoved)
            .Init();

        if (meshRenderingService.RegisterMeshGroup(Meshes.SM_Prop_CrossingLight_01, Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out var problems, out var groupHandle))
        {
            return problems.Prepend("Failed to register junction signal mesh group");
        }

        _groupHandle = groupHandle;

        foreach (var junctionId in junctionSignalService.SignalJunctions)
        {
            if (OnAdded(junctionId).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to add existing signal for junction '{0}'", junctionId);
            }
        }

        return Result.Success();
    }

    public Result Unload()
    {
        _junctionSignalAddedQueue.Cleanup();
        _junctionSignalRemovedQueue.Cleanup();

        return Result.Success();
    }

    private Result OnAdded(Id<Junction> junctionId)
    {
        if (_instanceIds.ContainsKey(junctionId))
        {
            return new ResultProblem("Tried to add rendering instance of junction that already has rendering instance");
        }

        if (!junctionService.TryGetJunction(junctionId, out var junction))
        {
            return new ResultProblem("Junction not found: '{0}'", junctionId);
        }

        var junctionWorld = Matrix4X4.CreateScale(0.5f)
                            * Matrix4X4.CreateRotationY(float.Pi)
                            * Matrix4X4.CreateTranslation(0.3f, 0f, 0.3f)
                            * Matrix4X4.CreateTranslation(gridService.ToTileCenter(junction.Position));

        if (meshRenderingService.AddInstance(_groupHandle, junctionWorld)
            .TryPickProblems(out var problems, out var instanceHandle))
        {
            return problems.Prepend("Failed to add junction signal instance for '{0}'", junctionId);
        }

        _instanceIds[junctionId] = instanceHandle;

        return Result.Success();
    }

    private Result OnRemoved(Id<Junction> junctionId)
    {
        if (!_instanceIds.Remove(junctionId, out var instanceHandle))
        {
            return new ResultProblem("Could not find rendering instance for signal with junction id '{0}'", junctionId);
        }

        return meshRenderingService.RemoveInstance(instanceHandle);
    }

    public Result Update(TimeSpan deltaTime)
    {
        _junctionSignalRemovedQueue.Update();
        _junctionSignalAddedQueue.Update();

        return Result.Success();
    }
}
