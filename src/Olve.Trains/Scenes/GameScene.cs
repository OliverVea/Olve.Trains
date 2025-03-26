using System.Drawing;
using Olve.CodeGen;
using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes;

public sealed class GameScene : Scene
{
    public static readonly SceneId SceneId = new("Game Scene");
    public override SceneId Id => SceneId;

    private IsometricOrthographicCameraController _cameraController = null!;
    private readonly List<ICameraScheme> _cameraSchemes = [];
    private CameraMovementInput _cameraMovementInput = new();

    private RenderingId<MeshData> _meshRenderingId;
    private RenderingId<ShaderData> _shaderRenderingId;
    private RenderingId<TextureData> _textureRenderingId;

    private RenderingId<HeightmapData> _terrainRenderingId;
    private RenderingId<ShaderData> _terrainShaderRenderingId;

    private RenderingInstanceId _cubeInstanceId;
    private RenderingInstanceId _terrainInstanceId;


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

        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(0.701f, -1, 0.701f);

        const float orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(cameraTarget, cameraViewDirection, orthographicSize);
        _cameraSchemes.Add(new WasdMovement());

        _meshRenderingId = GameManager.MeshEntityManager.Register(trainMesh).Value;

        if (RegisterEntities(trainMesh, trainTexture, GameSceneEntities.DefaultShader.ShaderData).TryPickProblems(out problems, out var renderingEntityIds))
        {
            return problems;
        }

        if (RegisterTerrainEntities(terrain.Heightmap, GameSceneEntities.TerrainShader.ShaderData).TryPickProblems(out problems, out var terrainRenderingEntityIds))
        {
            return problems;
        }

        (_meshRenderingId, _textureRenderingId, _shaderRenderingId) = renderingEntityIds;
        (_terrainRenderingId, _terrainShaderRenderingId) = terrainRenderingEntityIds;

        GameSceneEntities.DefaultShader.RenderingId = _shaderRenderingId;
        GameSceneEntities.TerrainShader.RenderingId = _terrainShaderRenderingId;
        GameSceneEntities.TerrainShader.TexelSize = new Vector2D<float>(
            1f / terrain.Heightmap.Width,
            1f / terrain.Heightmap.Length
            );

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

        if (RegisterCubeInstance().TryPickProblems(out problems, out _cubeInstanceId))
        {
            return problems;
        }

        if (RegisterTerrainInstance().TryPickProblems(out problems, out _terrainInstanceId))
        {
            return problems;
        }
        
        // MSAA
        GameManager.GL.Enable(EnableCap.Multisample);
        GameManager.GL.Disable(EnableCap.CullFace);
        GameManager.GL.Enable(EnableCap.DepthTest);

        //GameManager.GL.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);

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

    private Result<RenderingInstanceId> RegisterTerrainInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        return GameManager.RenderingManager.RegisterInstance(_terrainRenderingId, _terrainShaderRenderingId, transform);
    }

    public override Result<PassInput> Input()
    {
        _cameraMovementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _cameraMovementInput += scheme.GetMovementInput();
        }

        return PassInput.Pass;
    }

    private float _lastTps;
    private float _t;
    private float _lastFps;
    private float _sunYAngle;

    // Degrees per second
    private const float _sunYAngleSpeed = 45f;

    private const float _sunXAngle = 15;

    private Vector3D<float> GetSunPosition()
    {
        var xRadians = float.DegreesToRadians(_sunXAngle);
        var yRadians = float.DegreesToRadians(_sunYAngle);

        Vector3D<float> sunPosition = Vector3D<float>.UnitY;

        sunPosition = Vector3D.Transform(sunPosition,
            Matrix4X4.CreateRotationX(xRadians) * Matrix4X4.CreateRotationY(yRadians));

        return sunPosition;
    }


    private Vector3D<float> GetSunDirection()
    {
        return Vector3D.Normalize(-GetSunPosition());
    }

    public override Result Update(TimeSpan deltaTime)
    {
        var dt = deltaTime.InSeconds();
        _t += dt;

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
            Console.WriteLine($"TPS: {1 / dt}");
        }

        _sunYAngle += _sunYAngleSpeed * dt;
        while (_sunYAngle > 360)
        {
            _sunYAngle -= 360;
        }

        _cameraController.Move(_cameraMovementInput.Direction, deltaTime);
        _cameraController.Zoom(_cameraMovementInput.Zoom, deltaTime);
        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        var dt = deltaTime.InSeconds();

        if (_t - _lastFps > 1)
        {
            _lastFps = _t;
            Console.WriteLine($"FPS: {1 / dt}");
        }

        GameManager.GL.ClearColor(Color.CornflowerBlue);
        GameManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewMatrix = _cameraController.Camera.GetViewMatrix();
        var projectionMatrix = _cameraController.Camera.GetProjectionMatrix();

        GameSceneEntities.DefaultShader.View = viewMatrix;
        GameSceneEntities.DefaultShader.Projection = projectionMatrix;

        GameSceneEntities.TerrainShader.View = viewMatrix;
        GameSceneEntities.TerrainShader.Projection = projectionMatrix;

        var cameraRotationMatrix = viewMatrix.ExtractRotation();

        var cameraViewDirection = Vector3D.Transform(Vector3D<float>.UnitZ, cameraRotationMatrix);
        GameSceneEntities.TerrainShader.CameraDirection = cameraViewDirection;

        var sunDirection = GetSunDirection();

        GameSceneEntities.DefaultShader.DirectionalLightDir = sunDirection;
        GameSceneEntities.TerrainShader.DirectionalLightDir = sunDirection;


        var defaultShaderResult = GameManager.RenderingManager.Render(GameSceneEntities.DefaultShader);
        var terrainShaderResult = GameManager.RenderingManager.Render(GameSceneEntities.TerrainShader);


        if (defaultShaderResult.TryPickProblems(out var problems) || terrainShaderResult.TryPickProblems(out problems))
        {
            return problems.Prepend("Failed rendering game objects");
        }

        return Result.Success();
    }
}