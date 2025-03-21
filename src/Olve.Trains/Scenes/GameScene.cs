using System.Drawing;
using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Shaders;
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
    private readonly Random _random = new();

    public override Result Load()
    {
        if (ShaderLoader.Load(DefaultShaderData).TryPickProblems(out var shaderProblems, out var shaderData))
        {
            return shaderProblems.Prepend("Failed loading default shader");
        }

        var cube = new Model
        {
            Indices = CubeIndices,
            Vertices = CubeVertices,
            Normals = CubeNormals,
            ShaderData = shaderData
        };

        for (int i = 0; i < 10; i++)
        {
            var scale = _random.NextFloat(0.5f, 1.5f);
            var position = new Vector3D<float>(
                _random.NextFloat(-5f, 5f),  // Random X
                _random.NextFloat(-2f, 2f),  // Random Y
                _random.NextFloat(-5f, 5f)   // Random Z
            );

            var rotationSpeed = _random.NextFloat(0.5f, 20.0f); // Random rotation speed

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

    public override Result Update(TimeSpan deltaTime)
    {
        _cameraController.Move(_cameraMovementInput.Direction, deltaTime);
        _cameraController.Zoom(_cameraMovementInput.Zoom, deltaTime);
        return Result.Success();
    }

    private float _t = 0;

    private static readonly DirectionalLight DirectionalLight = new(Vector3D.Normalize(new Vector3D<float>(1, -1, 1)), new Vector3D<float>(1, 1, 0.8f), 1.0f);
    private static readonly AmbientLight AmbientLight = new(new Vector3D<float>(0.15f, 0.15f, 0.2f), 0.5f);

    public override Result Render(TimeSpan deltaTime)
    {
        var dt = deltaTime.InSeconds();
        _t += dt;

        GameManager.GL.ClearColor(Color.CornflowerBlue);
        GameManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewMatrix = _cameraController.Camera.GetViewMatrix();
        var projectionMatrix = _cameraController.Camera.GetProjectionMatrix();

        for (int i = 0; i < _rectangleInstances.Count; i++)
        {
            var rotationMatrix = Matrix4X4.CreateRotationY(dt * _rotationSpeeds[i]);

            if (GameManager.ModelRenderingManager.GetInstanceWorld(_rectangleInstances[i]).TryPickProblems(out var problems1, out var worldMatrix))
            {
                return problems1.Prepend("Could not get world matrix for rectangle instance {0}", i);
            }

            worldMatrix = rotationMatrix * worldMatrix;

            GameManager.ModelRenderingManager.SetInstanceWorld(_rectangleInstances[i], worldMatrix);
        }

        var rawMousePosition = GameManager.MouseManager.State.Position;
        var mousePosition = GameManager.Window.Size.ToNdc(rawMousePosition);

        if (_cameraController.Camera.GetRay(mousePosition).TryPickProblems(out var problems, out var ray))
        {
            return problems;
        }

        Console.WriteLine($"Mouse ray: ({ray.Origin.X}, {ray.Origin.Y}, {ray.Origin.Z}) -> ({ray.Direction.X}, {ray.Direction.Y}, {ray.Direction.Z})");

        ModelRenderingManager.RenderingParameters parameters = new(viewMatrix, projectionMatrix, ray, DirectionalLight, AmbientLight);
        var renderResult = GameManager.ModelRenderingManager.Render(parameters);
        if (renderResult.TryPickProblems(out problems))
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