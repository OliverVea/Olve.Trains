using System.Drawing;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Trains.Scenes.Game;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes;

public sealed class GameScene : Scene
{
    private readonly SceneLightService _lightService = new();
    private readonly CameraSceneService _cameraService = new();

    private ISceneService[] _services;

    public static readonly SceneId SceneId = new("Game Scene");
    public override SceneId Id => SceneId;

    private HeightmapRaycaster _heightmapRaycaster;

    private RenderingId<MeshData> _meshRenderingId;
    private RenderingId<ShaderData> _shaderRenderingId;
    private RenderingId<TextureData> _textureRenderingId;

    private RenderingId<HeightmapData> _terrainRenderingId;
    private RenderingId<ShaderData> _terrainShaderRenderingId;
    private RenderingId<ShaderData> _wireframeTerrainShaderId;

    private RenderingInstanceId _cubeInstanceId;

    private Ray3D<float>? _mouseRay = null;

    public override Result Load()
    {
        _services = [_lightService, _cameraService];

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

        _meshRenderingId = GameManager.MeshEntityManager.Register(trainMesh).Value;

        if (RegisterEntities(trainMesh, trainTexture, GameSceneEntities.DefaultShader.ShaderData).TryPickProblems(out problems, out var renderingEntityIds))
        {
            return problems;
        }

        if (RegisterTerrainEntities(terrain.Heightmap, GameSceneEntities.TerrainShader.ShaderData).TryPickProblems(out problems, out var terrainRenderingEntityIds))
        {
            return problems;
        }

        if (GameManager.ShaderEntityManager.Register(GameSceneEntities.TerrainWireframe.ShaderData).TryPickProblems(out problems, out var wireframeTerrainShaderId))
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


        if (GameManager.TextureEntityManager.GetRegistration(_textureRenderingId).TryPickProblems(out problems, out var textureData))
        {
            return problems;
        }

        if (GameManager.HeightmapEntityManager.GetRegistration(_terrainRenderingId).TryPickProblems(out problems, out var terrainRegistration))
        {
            return problems;
        }

        GameSceneEntities.DefaultShader.TextureSampler = textureData.Texture;
        GameSceneEntities.TerrainShader.HeightMap = terrainRegistration.Texture;
        GameSceneEntities.TerrainWireframe.HeightMap = terrainRegistration.Texture;

        if (RegisterCubeInstance().TryPickProblems(out problems, out _cubeInstanceId))
        {
            return problems;
        }

        if (RegisterTerrainInstance().TryPickProblems(out problems))
        {
            return problems;
        }
        
        // MSAA
        GameManager.GL.Enable(EnableCap.Multisample);
        GameManager.GL.Disable(EnableCap.CullFace);
        GameManager.GL.Enable(EnableCap.DepthTest);

        var loadServicesResult = _services.Select(x => x.Load());
        if (loadServicesResult.TryPickProblems(out problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result<(RenderingId<MeshData>, RenderingId<TextureData>, RenderingId<ShaderData>)> RegisterEntities(MeshData meshData, TextureData textureData, ShaderData shaderData)
    {
        return Result.Concat(
            () => GameManager.MeshEntityManager.Register(meshData),
            () => GameManager.TextureEntityManager.Register(textureData),
            () => GameManager.ShaderEntityManager.Register(shaderData));
    }

    private Result<(RenderingId<HeightmapData>, RenderingId<ShaderData>)> RegisterTerrainEntities(HeightmapData heightmapData, ShaderData shaderData)
    {
        return Result.Concat(
            () => GameManager.HeightmapEntityManager.Register(heightmapData),
            () => GameManager.ShaderEntityManager.Register(shaderData));
    }

    private Result<RenderingInstanceId> RegisterCubeInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        return GameManager.RenderingManager.RegisterInstance(_meshRenderingId, _shaderRenderingId, transform);
    }

    private Result RegisterTerrainInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        var terrainResult = GameManager.RenderingManager.RegisterInstance(_terrainRenderingId, _terrainShaderRenderingId, transform);
        var wireframeResult = GameManager.RenderingManager.RegisterInstance(_terrainRenderingId, _wireframeTerrainShaderId, transform);

        if (terrainResult.TryPickProblems(out var problems)
            || wireframeResult.TryPickProblems(out problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public override Result<Pass> Input()
    {
        var mouseCoordinates = GameManager.MouseManager.State.NormalizedPosition;

        if (mouseCoordinates.X is >= -1 and <= 1
            && mouseCoordinates.Y is >= -1 and <= 1
            && _cameraService.Camera.GetRay(mouseCoordinates).TryPickValue(out var mouseRay))
        {
            _mouseRay = mouseRay;
        }
        else
        {
            _mouseRay = null;
        }

        foreach (var service in _services)
        {
            var inputResult = service.Input();
            if (inputResult.TryPickProblems(out var problems, out var passInput))
            {
                return problems;
            }

            if (passInput == Pass.Block)
            {
                return Pass.Block;
            }
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
            //GameManager.RenderingManager.SetInstanceWorld(_cubeInstanceId, Matrix4X4.CreateTranslation(hit.Value));
            GameSceneEntities.TerrainWireframe.MousePosition = hit.Value;
        }

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
        }

        var updateServicesResult = _services.Select(x => x.Update(deltaTime));
        if (updateServicesResult.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        GameManager.GL.ClearColor(Color.CornflowerBlue);
        GameManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewMatrix = _cameraService.Camera.GetViewMatrix();
        var projectionMatrix = _cameraService.Camera.GetProjectionMatrix();

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

        var defaultShaderResult = GameManager.RenderingManager.Render(GameSceneEntities.DefaultShader).IfProblem(p => p.Prepend("Failed rendering game objects"));
        var terrainShaderResult = GameManager.RenderingManager.Render(GameSceneEntities.TerrainShader).IfProblem(p => p.Prepend("Failed rendering terrain"));
        var wireframeShaderResult = GameManager.RenderingManager.Render(GameSceneEntities.TerrainWireframe).IfProblem(p => p.Prepend("Failed rendering terrain wireframe"));

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