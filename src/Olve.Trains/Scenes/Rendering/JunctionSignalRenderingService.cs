using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Junctions;

namespace Olve.Trains.Scenes.Rendering;

public class JunctionSignalRenderingService(
    ILoggingManager loggingManager,
    CameraSceneService cameraSceneService,
    MeshEntityManager meshEntityManager,
    RenderingManager3D renderingManager3D,
    TextureEntityManager textureEntityManager,
    ShaderEntityManager shaderEntityManager,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService)
    : BaseEntityListeningService<Junction>(loggingManager, junctionSignalService)
{
    private RenderingId<MeshData> MeshRenderingId { get; set; }
    private readonly Dictionary<Id<Junction>, RenderingInstanceId> _instanceIds = new();
    private readonly Shaders.Default _shader = new();

    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (true, true);

    protected override Result OnLoad()
    {
        if (RenderingServiceHelper.LoadTexture(Textures.SimpleTrains_Texture_01, textureEntityManager)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        _shader.TextureSampler = textureId;

        if (RenderingServiceHelper.LoadShader(_shader, shaderEntityManager).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (AssetLoader.LoadAsset(Meshes.SM_Veh_Bullet_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        var meshRegistrationResult = meshEntityManager.Register(meshData);
        if (meshRegistrationResult.TryPickProblems(out problems, out var meshRenderingId))
        {
            return problems.Prepend("Failed to register mesh");
        }

        MeshRenderingId = meshRenderingId;

        return base.OnLoad();
    }

    protected override Result OnAdded(Id<Junction> junctionId)
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
                            * Matrix4X4.CreateTranslation(0, -0.9f, 0)
                            * junction.Position.ToWorldMatrix();

        var renderingResult = renderingManager3D.RegisterInstance(MeshRenderingId, _shader.RenderingId, junctionWorld);
        if (renderingResult.TryPickProblems(out problems, out var junctionInstanceId))
        {
            return problems;
        }

        _instanceIds[junctionId] = junctionInstanceId;
        return Result.Success();
    }

    protected override Result OnRemoved(Id<Junction> junctionId)
    {
        if (!_instanceIds.TryGetValue(junctionId, out var instanceId))
        {
            return new ResultProblem("Could not find rendering instance for signal with junction id '{0}'", junctionId);
        }

        return renderingManager3D.DeregisterInstance(instanceId);
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        cameraSceneService.ApplyCameraPositionParameters(_shader);

        return base.OnUpdate(deltaTime);
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        return renderingManager3D.Render(_shader);
    }
}