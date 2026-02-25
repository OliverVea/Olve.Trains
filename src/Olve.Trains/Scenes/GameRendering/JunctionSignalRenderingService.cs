using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Junctions;

namespace Olve.Trains.Scenes.GameRendering;

public class JunctionSignalRenderingService(
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    RenderingServiceHelper renderingServiceHelper,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    GridService gridService)
    : ISceneService
{
    private readonly Dictionary<Id<Junction>, Id<Shaders.Default.Instance>> _instanceIds = new();

    private readonly Shaders.Default _shader = new()
    {
        UColor = new Vector3D<float>(1f, 1f, 1f),
        UOpacity = 1.0f,
        UColorMix = 0f,
    };

    private readonly EventQueue<Id<Junction>> _junctionSignalAddedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionAdded);
    private readonly EventQueue<Id<Junction>> _junctionSignalRemovedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionRemoved);

    private GroupId<Shaders.Default.Instance> _groupId = null!;

    public Result Load()
    {
        _junctionSignalAddedQueue
            .SetHandler(OnAdded)
            .Init();
        _junctionSignalRemovedQueue
            .SetHandler(OnRemoved)
            .Init();

        if (textureLoadingManager.LoadTexture(Textures.SimpleTrains_Texture_01)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(textureId, new TextureUploadOptions())
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register texture with OpenGL");
        }

        _shader.TextureSampler = textureId;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Prop_CrossingLight_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        // Populate typed vertices from mesh data
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        meshData.Populate(vertices);

        // Extract indices
        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        // Register geometry
        if (geometryManager.Register<Shaders.Default.Vertex>(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register junction signal geometry");
        }

        // Register group
        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance>(
                geometryId, _shader, RenderState.Opaque)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register junction signal group");
        }

        _groupId = groupId;

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

        var junctionWorld = Matrix4X4.CreateScale(0.2f)
                            * Matrix4X4.CreateRotationY(float.Pi)
                            * Matrix4X4.CreateTranslation(-0.2f, -0.9f, -0.2f)
                            * Matrix4X4.CreateTranslation(gridService.ToTileCenter(junction.Position));

        if (renderingInstanceManager.Add(_groupId, new Shaders.Default.Instance(junctionWorld))
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add junction signal instance for '{0}'", junctionId);
        }

        _instanceIds[junctionId] = instanceId;

        return Result.Success();
    }

    private Result OnRemoved(Id<Junction> junctionId)
    {
        if (!_instanceIds.Remove(junctionId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for signal with junction id '{0}'", junctionId);
        }

        return renderingInstanceManager.Remove(_groupId, instanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        _junctionSignalRemovedQueue.Update();
        _junctionSignalAddedQueue.Update();

        return Result.Success();
    }
}
