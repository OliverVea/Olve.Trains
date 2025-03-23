using System.Drawing;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Scenes;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Utilities;

public partial class SandboxScene : Scene
{
    private GLShader _rayGLShader = null!;

    public static readonly SceneId SceneId = new("SandboxScene");
    public override SceneId Id => SceneId;

    private PerspectiveCameraController _perspectiveCameraController = null!;
    private readonly List<ICameraScheme> _cameraSchemes = [];

    private readonly Queue<Ray3D<float>> _rays = new();

    private CameraMovementInput _cameraMovementInput = new();

    public override Result Load()
    {
        if (GameManager.ModelRenderingManager.Register(Cube).TryPickProblems(out var modelProblems, out var cubeRenderingId))
        {
            return modelProblems.Prepend("Failed registering cube model");
        }

        GameManager.ModelRenderingManager.RegisterInstance(cubeRenderingId, Matrix4X4<float>.Identity);

        FirstPersonView view = new() { Position = new Vector3D<float>(0, 0, -5) };
        PerspectiveProjection projection = new() { AspectRatio = 16f / 9f, NearPlane = 0.1f, FarPlane = 1000 };

        var camera = Camera.Camera.Create(view, projection);

        _perspectiveCameraController = new PerspectiveCameraController(camera);

        _cameraSchemes.Add(new WasdMovement());
        _cameraSchemes.Add(new MouseLook());

        _rayGLShader.ViewMatrixUniformName = "view";
        _rayGLShader.ProjectionMatrixUniformName = "proj";

        return Result.Success();
    }

    public override Result<PassInput> Input()
    {
        _cameraMovementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _cameraMovementInput += scheme.GetMovementInput();
        }

        if (GameManager.MouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            var rayResult = _perspectiveCameraController.Camera.GetRay(new Vector2D<float>(0f, 0f));
            if (rayResult.TryPickProblems(out var problems, out var ray))
            {
                return problems.Prepend("Failed creating ray");
            }

            _rays.Enqueue(ray);
            if (_rays.Count > 10)
            {
                _rays.Dequeue();
            }
        }

        if (GameManager.KeyboardManager.State.IsKeyPressed(Key.C))
        {
            _rays.Clear();
        }

        return PassInput.Pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        _perspectiveCameraController.Move(_cameraMovementInput.Direction, deltaTime);
        _perspectiveCameraController.Rotate(_cameraMovementInput.Rotation, deltaTime);
        _perspectiveCameraController.Zoom(_cameraMovementInput.Zoom, deltaTime);

        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        GameManager.GL.ClearColor(Color.CornflowerBlue);
        GameManager.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewMatrix = _perspectiveCameraController.Camera.GetViewMatrix();
        var projectionMatrix = _perspectiveCameraController.Camera.GetProjectionMatrix();

        RenderingParameters parameters = new([
            new RenderingParameter.Matrix4X4("view", viewMatrix),
            new RenderingParameter.Matrix4X4("projection", projectionMatrix)
        ]);
        var renderResult = GameManager.ModelRenderingManager.Render(parameters);

        if (renderResult.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed rendering game objects");
        }

        foreach (var ray in _rays)
        {
            ray.Render(_rayGLShader, viewMatrix, projectionMatrix);
        }

        return Result.Success();
    }
}