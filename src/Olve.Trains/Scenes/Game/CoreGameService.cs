using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;
public sealed class CoreGameService(CameraSceneService cameraSceneService,
    MeshEntityManager meshEntityManager,
    ShaderEntityManager shaderEntityManager,
    TextureEntityManager textureEntityManager,
    RenderingManager3D renderingManager3D,
    MouseManager mouseManager) : SceneService
{
    private HeightmapRaycaster _heightmapRaycaster = null!;

    private RenderingId<MeshData> _meshRenderingId;
    private RenderingId<ShaderData> _shaderRenderingId;
    private RenderingId<TextureData> _textureRenderingId;

    private Ray3D<float>? _mouseRay;

    public override Result Load()
    {
        var trainMeshResult = AssetLoader.LoadAsset(Meshes.SM_Veh_Bullet_01);
        if (trainMeshResult.TryPickProblems(out var problems, out var trainMesh))
        {
            return problems;
        }

        var trainTextureResult = AssetLoader.LoadAsset(Textures.SimpleTrains_Texture_01);
        if (trainTextureResult.TryPickProblems(out problems, out var trainTexture))
        {
            return problems;
        }

        var terrainResult = AssetLoader.LoadAsset(Terrains.terrain01);
        if (terrainResult.TryPickProblems(out problems, out var terrain))
        {
            return problems;
        }

        _heightmapRaycaster = new HeightmapRaycaster(terrain.Heightmap);

        _meshRenderingId = meshEntityManager.Register(trainMesh).Value;

        if (RegisterEntities(trainMesh, trainTexture, GameSceneEntities.DefaultShader.ShaderData).TryPickProblems(out problems, out var renderingEntityIds))
        {
            return problems;
        }

        (_meshRenderingId, _textureRenderingId, _shaderRenderingId) = renderingEntityIds;

        GameSceneEntities.DefaultShader.RenderingId = _shaderRenderingId;


        if (textureEntityManager.GetRegistration(_textureRenderingId).TryPickProblems(out problems, out var textureData))
        {
            return problems;
        }

        GameSceneEntities.DefaultShader.TextureSampler = textureData.Texture;

        if (RegisterCubeInstance().TryPickProblems(out problems, out _))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result<(RenderingId<MeshData>, RenderingId<TextureData>, RenderingId<ShaderData>)> RegisterEntities(MeshData meshData, TextureData textureData, ShaderData shaderData)
    {
        return Result.Concat(
            () => meshEntityManager.Register(meshData),
            () => textureEntityManager.Register(textureData),
            () => shaderEntityManager.Register(shaderData));
    }

    private Result<RenderingInstanceId> RegisterCubeInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        return renderingManager3D.RegisterInstance(_meshRenderingId, _shaderRenderingId, transform);
    }

    public override Result<Pass> Input(TimeSpan deltaTime)
    {
        var mouseCoordinates = mouseManager.State.NormalizedPosition;

        if (mouseCoordinates.X is >= -1 and <= 1
            && mouseCoordinates.Y is >= -1 and <= 1
            && cameraSceneService.Camera.GetRay(mouseCoordinates).TryPickValue(out var mouseRay))
        {
            _mouseRay = mouseRay;
        }
        else
        {
            _mouseRay = null;
        }

        return Pass.Pass;
    }

    private float _lastTps;
    private float _t;
    private readonly float _speedScalar = 1f;

    public override Result Update(TimeSpan deltaTime)
    {
        deltaTime *= _speedScalar;

        var dt = deltaTime.InSeconds();

        _t += dt;

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
        }

        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        GameSceneEntities.DefaultShader.View = cameraSceneService.ViewMatrix;
        GameSceneEntities.DefaultShader.Projection = cameraSceneService.ProjectionMatrix;

        GameSceneEntities.DefaultShader.CameraDirection = cameraSceneService.CameraViewDirection;

        var defaultShaderResult = renderingManager3D.Render(GameSceneEntities.DefaultShader).IfProblem(p => p.Prepend("Failed rendering game objects"));

        if (defaultShaderResult.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }
}