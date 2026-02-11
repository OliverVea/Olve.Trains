using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.Game.Junctions;

namespace Olve.Trains.Scenes.Rendering;

public class JunctionSignalRenderingService(
    ILogger<JunctionSignalRenderingService> logger,
    EventQueueFactory eventQueueFactory,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    RenderingServiceHelper renderingServiceHelper,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService)
    : ISceneService
{
    private GeometryId _geometryId;
    private readonly Dictionary<Id<Junction>, RenderingInstanceId> _instanceIds = new();
    private readonly Shaders.Default _shader = new();

    private readonly EventQueue<Id<Junction>> _junctionSignalAddedQueue = eventQueueFactory.Create(junctionSignalService.OnAdded);
    private readonly EventQueue<Id<Junction>> _junctionSignalRemovedQueue = eventQueueFactory.Create(junctionSignalService.OnRemoved);

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

        // TODO: Improve this mapping - perhaps generalize
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
            vertices[i] = new(meshData.Positions[i], meshData.Normals[i], meshData.TextureCoordinates[i]);

        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        if (renderingManager3D.RegisterGeometry(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register geometry");
        }

        _geometryId = geometryId;

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

        var deregisterInstanceResults = _instanceIds.Select(ids => renderingManager3D.DeregisterInstance(ids.Value));

        var result = Result.Concat([
            ..deregisterInstanceResults,
            renderingManager3D.DeregisterGeometry(_geometryId),
            renderingServiceHelper.UnloadShader(_shader),
        ]);

        if (result.TryPickProblems(out var problems))
        {
            logger.Log(problems.Prepend("Failed to deregister rendering resources owned by {0}", nameof(JunctionSignalRenderingService)));
        }

        return Result.Success();
    }

    private Result OnAdded(Id<Junction> junctionId)
    {
        if (_instanceIds.ContainsKey(junctionId))
        {
            return new ResultProblem("Tried to add rendering instance of junction that already has rendering instance");
        }

        if (junctionService.Get(junctionId).TryPickProblems(out var problems, out var junction))
        {
            return problems;
        }

        var junctionWorld = Matrix4X4.CreateScale(0.2f)
                            * Matrix4X4.CreateRotationY(float.Pi)
                            * Matrix4X4.CreateTranslation(-0.2f, -0.9f, -0.2f)
                            * junction.Position.ToWorldMatrix();

        var renderingResult = renderingManager3D.RegisterInstance(_geometryId, _shader.RenderingId, junctionWorld);
        if (renderingResult.TryPickProblems(out problems, out var junctionInstanceId))
        {
            return problems;
        }

        _instanceIds[junctionId] = junctionInstanceId;
        return Result.Success();
    }

    private Result OnRemoved(Id<Junction> junctionId)
    {
        if (!_instanceIds.TryGetValue(junctionId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for signal with junction id '{0}'", junctionId);
        }

        return renderingManager3D.DeregisterInstance(instanceId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        _junctionSignalRemovedQueue.Update();
        _junctionSignalAddedQueue.Update();

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        return renderingManager3D.Render(_shader);
    }
}