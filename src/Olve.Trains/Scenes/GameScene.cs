using System.Drawing;
using Olve.CodeGen.Shaders;
using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using static Olve.Trains.Scenes.GameSceneEntities;

namespace Olve.Trains.Scenes;

public sealed class GameScene : Scene
{
    public static readonly SceneId SceneId = new("Game Scene");
    public override SceneId Id => SceneId;

    private IsometricOrthographicCameraController _cameraController = null!;
    private readonly List<ICameraScheme> _cameraSchemes = [];
    private CameraMovementInput _cameraMovementInput = new();

    private readonly List<RenderingInstanceId<Model>> _rectangleInstances = [];
    private readonly List<float> _rotationSpeeds = [];
    private readonly List<Vector3D<float>> _rotationAxes = new();
    private readonly Random _random = new();

    private readonly Shaders.Default _defaultShader = new(
            ambientLightColor: new Vector3D<float>(180, 167, 214) / 255f,
            ambientLightIntensity: 0.2f,
            directionalLightColor: new Vector3D<float>(249,233,164) / 255f,
            directionalLightDir: Vector3D.Normalize(new Vector3D<float>(0.2f, -1, 0.2f)),
            directionalIntensity: 1.0f,
            world: Matrix4X4<float>.Identity,
            view: Matrix4X4<float>.Identity,
            projection: Matrix4X4<float>.Identity
        );

    public override Result Load()
    {
        var cube = new Model
        {
            Mesh = new()
            {
                Indices = CubeIndices,
                Vertices = CubeVertices,
                Normals = CubeNormals,
            }, 
            ShaderData = _defaultShader.ShaderData
        };

        const int CubeCount = 100;
        for (int i = 0; i < CubeCount; i++)
        {
            var scale = _random.NextFloat(0.5f, 1.5f);
            var position = new Vector3D<float>(
                _random.NextFloat(-5f, 5f),  // Random X
                _random.NextFloat(-2f, 2f),  // Random Y
                _random.NextFloat(-5f, 5f)   // Random Z
            );
            
            var axis = new Vector3D<float>(
                _random.NextFloat(-1f, 1f),
                _random.NextFloat(-1f, 1f),
                _random.NextFloat(-1f, 1f)
            );
            axis = Vector3D.Normalize(axis);
            _rotationAxes.Add(axis);

            var rotationSpeed = _random.NextFloat(0.1f, 0.6f); // Random rotation speed

            var scaleMatrix = Matrix4X4.CreateScale(scale);
            var translationMatrix = Matrix4X4.CreateTranslation(position);
            var transform = scaleMatrix * translationMatrix;

            if (RegisterModel(cube, transform).TryPickProblems(out var modelProblems, out var instanceId))
            {
                return modelProblems.Prepend($"Failed registering rectangle model {i}");
            }

            _rectangleInstances.Add(instanceId);
            _rotationSpeeds.Add(rotationSpeed);
        }

        var cameraTarget = new Vector3D<float>(0, 0, 0);
        var cameraViewDirection = new Vector3D<float>(1, -1, 1);
        var orthographicSize = 10f;

        _cameraController = IsometricOrthographicCameraController.Create(cameraTarget, cameraViewDirection, orthographicSize);
        _cameraSchemes.Add(new WasdMovement());
        
        // MSAA
        GameManager.GL.Enable(EnableCap.Multisample);

        return Result.Success();
    }

    private Result<RenderingInstanceId<Model>> RegisterModel(Model model, Matrix4X4<float> transform)
    {
        return Result.Chain(
            () => GameManager.ModelRenderingManager.Register(model),
            id => GameManager.ModelRenderingManager.RegisterInstance(id, transform));
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

    private float _lastTps = 0;
    private float _t = 0;
    private float _lastFps = 0;

    public override Result Update(TimeSpan deltaTime)
    {
        var dt = deltaTime.InSeconds();
        _t += dt;

        if (_t - _lastTps > 1)
        {
            _lastTps = _t;
            Console.WriteLine($"TPS: {1 / dt}");
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

        for (int i = 0; i < _rectangleInstances.Count; i++)
        {
            var rotationMatrix = Matrix4X4.CreateFromAxisAngle(_rotationAxes[i], dt * _rotationSpeeds[i]);

            if (GameManager.ModelRenderingManager.GetInstanceWorld(_rectangleInstances[i]).TryPickProblems(out var problems1, out var worldMatrix))
            {
                return problems1.Prepend("Could not get world matrix for rectangle instance {0}", i);
            }

            worldMatrix = rotationMatrix * worldMatrix;

            GameManager.ModelRenderingManager.SetInstanceWorld(_rectangleInstances[i], worldMatrix);
        }
        
        _defaultShader.View = viewMatrix;
        _defaultShader.Projection = projectionMatrix;
        
        RenderingParameters parameters = new (_defaultShader.MakeParameters());

        var renderResult = GameManager.ModelRenderingManager.Render(parameters);
        if (renderResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed rendering game objects");
        }

        return Result.Success();
    }
}

// Extension method for generating random floats
public static class RandomExtensions
{
    public static float NextFloat(this Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}

public static class ScreenSizeExtensions
{
    public static Vector2D<float> ToNdc(this Vector2D<int> screenSize, Vector2D<float> screenPosition)
    {
        return new Vector2D<float>(
            (2.0f * screenPosition.X) / screenSize.X - 1.0f,
            1.0f - (2.0f * screenPosition.Y) / screenSize.Y
        );
    }
}