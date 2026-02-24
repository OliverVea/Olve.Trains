using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameRendering;

public class JunctionSignalRenderingService(
    ILogger<JunctionSignalRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    RenderingServiceHelper renderingServiceHelper,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    GridService gridService)
    : ISceneService
{
    private readonly Dictionary<Id<Junction>, Shaders.Default.Instance> _instances = new();
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;
    private bool _dirty;

    private readonly Shaders.Default _shader = new();

    private readonly EventQueue<Id<Junction>> _junctionSignalAddedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionAdded);
    private readonly EventQueue<Id<Junction>> _junctionSignalRemovedQueue = eventQueueFactory.Create(junctionSignalService.OnJunctionRemoved);

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

        var (vertexFloats, indices) = MeshDataMarshalHelper.Marshal<Shaders.Default.Vertex>(meshData);

        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                (uint)meshData.VertexCount,
                indices,
                Shaders.Default.Vertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Default.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create junction signal instanced buffers");
        }

        _registration = registration;

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

        instancedBufferManager.DeleteMeshInstanceBuffers(_registration);

        var result = renderingServiceHelper.UnloadShader(_shader);
        if (result.TryPickProblems(out var problems))
        {
            logger.Log(problems.Prepend("Failed to deregister rendering resources owned by {0}", nameof(JunctionSignalRenderingService)));
        }

        return Result.Success();
    }

    private Result OnAdded(Id<Junction> junctionId)
    {
        if (_instances.ContainsKey(junctionId))
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

        _instances[junctionId] = new Shaders.Default.Instance(junctionWorld);
        _dirty = true;

        return Result.Success();
    }

    private Result OnRemoved(Id<Junction> junctionId)
    {
        if (!_instances.ContainsKey(junctionId))
        {
            return new ResultProblem("Could not find rendering instance for signal with junction id '{0}'", junctionId);
        }

        _instances.Remove(junctionId);
        _dirty = true;

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        _junctionSignalRemovedQueue.Update();
        _junctionSignalAddedQueue.Update();

        if (_dirty)
        {
            RebuildInstanceBuffer();
            _dirty = false;
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        if (_instances.Count > 0)
        {
            if (renderingManager3D.RenderInstanced(_shader, _registration, (uint)_instances.Count)
                .TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }

    private void RebuildInstanceBuffer()
    {
        var instanceCount = _instances.Count;
        if (instanceCount == 0)
        {
            instancedBufferManager.UpdateMeshInstanceBuffer(
                _registration,
                ReadOnlySpan<float>.Empty,
                0,
                BufferUsageARB.DynamicDraw);
            return;
        }

        var floats = new float[instanceCount * Shaders.Default.Instance.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var instance in _instances.Values)
        {
            instance.WriteTo(span.Slice(offset, Shaders.Default.Instance.FloatCount));
            offset += Shaders.Default.Instance.FloatCount;
        }

        instancedBufferManager.UpdateMeshInstanceBuffer(
            _registration,
            floats,
            (uint)instanceCount,
            BufferUsageARB.DynamicDraw);
    }
}
