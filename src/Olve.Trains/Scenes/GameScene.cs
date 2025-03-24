using System.Drawing;
using Olve.CodeGen;
using Olve.Engine3D;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Rendering;
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

    private MeshRenderingId _meshRenderingId;
    private ShaderRenderingId _shaderRenderingId;
    private TextureRenderingId _textureRenderingId;

    private RenderingInstanceId _cubeInstanceId;

    private float _rotationAngle = 0f;


    public override Result Load()
    {
        var trainMeshResult = Meshes.LoadModelMesh(Meshes.SM_Veh_Bullet_01);
        if (trainMeshResult.TryPickProblems(out var problems, out var trainMesh))
        {
            return problems;
        }

        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(1, -1, 1);

        var orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(cameraTarget, cameraViewDirection, orthographicSize);
        _cameraSchemes.Add(new WasdMovement());

        _meshRenderingId = GameManager.MeshEntityManager.Register(trainMesh).Value;

        if (RegisterCubeEntities().TryPickProblems(out problems, out var renderingEntityIds))
        {
            return problems;
        }

        (_meshRenderingId, _textureRenderingId, _shaderRenderingId) = renderingEntityIds;

        GameSceneEntities.DefaultShader.TextureSampler = _textureRenderingId.Texture;

        if (RegisterCubeInstance().TryPickProblems(out problems, out _cubeInstanceId))
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

    private Result<(MeshRenderingId, TextureRenderingId, ShaderRenderingId)> RegisterCubeEntities()
    {
        return Result.Concat(
            () => GameManager.MeshEntityManager.Register(GameSceneEntities.Cube),
            () => GameManager.TextureEntityManager.Register(GameSceneEntities.DefaultTexture),
            () => GameManager.ShaderEntityManager.Register(GameSceneEntities.DefaultShader.ShaderData));
    }

    private Result<RenderingInstanceId> RegisterCubeInstance()
    {
        var transform = Matrix4X4<float>.Identity;

        transform *= 10;
        transform.M44 = 1;

        return GameManager.RenderingManager.RegisterInstance(_meshRenderingId, _shaderRenderingId, transform);
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

    public override Result Update(TimeSpan deltaTime)
    {
        var dt = deltaTime.InSeconds();
        _t += dt;

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
            Console.WriteLine($"TPS: {1 / dt}");
        }

        // Rotate cube
        _rotationAngle += 90f * dt; // Rotates 90 degrees per second
        if (_rotationAngle > 360f)
        {
            _rotationAngle -= 360f;
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
        RenderingParameters parameters = new (GameSceneEntities.DefaultShader.MakeParameters());

        Matrix4X4<float> cubeWorldMatrix = Matrix4X4.CreateFromAxisAngle(Vector3D<float>.UnitY, float.DegreesToRadians(_rotationAngle));

        GameManager.RenderingManager.SetInstanceWorld(_cubeInstanceId, cubeWorldMatrix);

        var renderResult = GameManager.RenderingManager.Render(parameters);
        if (renderResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed rendering game objects");
        }

        return Result.Success();
    }
}