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
    HeightmapEntityManager heightmapEntityManager,
    RenderingManager renderingManager,
    MouseManager mouseManager) : SceneService
{
    private HeightmapRaycaster _heightmapRaycaster = null!;

    private RenderingId<MeshData> _meshRenderingId;
    private RenderingId<ShaderData> _shaderRenderingId;
    private RenderingId<TextureData> _textureRenderingId;

    private RenderingId<HeightmapData> _terrainRenderingId;
    private RenderingId<ShaderData> _terrainShaderRenderingId;
    private RenderingId<ShaderData> _wireframeTerrainShaderId;

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

        if (RegisterTerrainEntities(terrain.Heightmap, GameSceneEntities.TerrainShader.ShaderData).TryPickProblems(out problems, out var terrainRenderingEntityIds))
        {
            return problems;
        }

        if (shaderEntityManager.Register(GameSceneEntities.TerrainWireframe.ShaderData).TryPickProblems(out problems, out var wireframeTerrainShaderId))
        {
            return problems;
        }

        (_meshRenderingId, _textureRenderingId, _shaderRenderingId) = renderingEntityIds;
        (_terrainRenderingId, _terrainShaderRenderingId) = terrainRenderingEntityIds;
        _wireframeTerrainShaderId = wireframeTerrainShaderId;

        GameSceneEntities.DefaultShader.RenderingId = _shaderRenderingId;
        GameSceneEntities.TerrainShader.RenderingId = _terrainShaderRenderingId;
        GameSceneEntities.TerrainWireframe.RenderingId = _wireframeTerrainShaderId;

        Vector2D<float> textureSize = new(1f / terrain.Heightmap.Width, 1f / terrain.Heightmap.Length);

        GameSceneEntities.TerrainShader.TexelSize = textureSize;
        GameSceneEntities.TerrainWireframe.TexelSize = textureSize;


        if (textureEntityManager.GetRegistration(_textureRenderingId).TryPickProblems(out problems, out var textureData))
        {
            return problems;
        }

        if (heightmapEntityManager.GetRegistration(_terrainRenderingId).TryPickProblems(out problems, out var terrainRegistration))
        {
            return problems;
        }

        GameSceneEntities.DefaultShader.TextureSampler = textureData.Texture;
        GameSceneEntities.TerrainShader.HeightMap = terrainRegistration.Texture;
        GameSceneEntities.TerrainWireframe.HeightMap = terrainRegistration.Texture;

        if (RegisterCubeInstance().TryPickProblems(out problems, out _))
        {
            return problems;
        }

        if (RegisterTerrainInstance().TryPickProblems(out problems))
        {
            return problems;
        }
        
        // MSAA

        return Result.Success();
    }

    private Result<(RenderingId<MeshData>, RenderingId<TextureData>, RenderingId<ShaderData>)> RegisterEntities(MeshData meshData, TextureData textureData, ShaderData shaderData)
    {
        return Result.Concat(
            () => meshEntityManager.Register(meshData),
            () => textureEntityManager.Register(textureData),
            () => shaderEntityManager.Register(shaderData));
    }

    private Result<(RenderingId<HeightmapData>, RenderingId<ShaderData>)> RegisterTerrainEntities(HeightmapData heightmapData, ShaderData shaderData)
    {
        return Result.Concat(
            () => heightmapEntityManager.Register(heightmapData),
            () => shaderEntityManager.Register(shaderData));
    }

    private Result<RenderingInstanceId> RegisterCubeInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        return renderingManager.RegisterInstance(_meshRenderingId, _shaderRenderingId, transform);
    }

    private Result RegisterTerrainInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        var terrainResult = renderingManager.RegisterInstance(_terrainRenderingId, _terrainShaderRenderingId, transform);
        var wireframeResult = renderingManager.RegisterInstance(_terrainRenderingId, _wireframeTerrainShaderId, transform);

        if (terrainResult.TryPickProblems(out var problems)
            || wireframeResult.TryPickProblems(out problems))
        {
            return problems;
        }

        return Result.Success();
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

        if (_mouseRay.HasValue && _heightmapRaycaster.TryRaycast(_mouseRay.Value, out var hit))
        {
            GameSceneEntities.TerrainWireframe.MousePosition = hit.Value;
        }

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
        }

        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {

        var viewMatrix = cameraSceneService.Camera.GetViewMatrix();
        var projectionMatrix = cameraSceneService.Camera.GetProjectionMatrix();

        GameSceneEntities.DefaultShader.View = viewMatrix;
        GameSceneEntities.DefaultShader.Projection = projectionMatrix;

        GameSceneEntities.TerrainShader.View = viewMatrix;
        GameSceneEntities.TerrainShader.Projection = projectionMatrix;

        GameSceneEntities.TerrainWireframe.View = viewMatrix;
        GameSceneEntities.TerrainWireframe.Projection = projectionMatrix;

        var cameraRotationMatrix = viewMatrix.ExtractRotation();

        var cameraViewDirection = Vector3D.Transform(Vector3D<float>.UnitZ, cameraRotationMatrix);
        GameSceneEntities.DefaultShader.CameraDirection = cameraViewDirection;
        GameSceneEntities.TerrainShader.CameraDirection = cameraViewDirection;

        var defaultShaderResult = renderingManager.Render(GameSceneEntities.DefaultShader).IfProblem(p => p.Prepend("Failed rendering game objects"));
        var terrainShaderResult = renderingManager.Render(GameSceneEntities.TerrainShader).IfProblem(p => p.Prepend("Failed rendering terrain"));
        var wireframeShaderResult = renderingManager.Render(GameSceneEntities.TerrainWireframe).IfProblem(p => p.Prepend("Failed rendering terrain wireframe"));

        if (defaultShaderResult.TryPickProblems(out var problems)
            || terrainShaderResult.TryPickProblems(out problems)
            || wireframeShaderResult.TryPickProblems(out problems)
            )
        {
            return problems;
        }

        return Result.Success();
    }
}