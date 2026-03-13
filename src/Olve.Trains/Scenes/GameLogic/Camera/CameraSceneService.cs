using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Events;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameLogic.ShaderExtensions;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.GameLogic.Camera;

public class CameraSceneService(Provider<IWindow> windowProvider, KeyboardManager keyboardManager, ScreenResizedEvent screenResizedEvent) : ISceneService
{
    private IsometricOrthographicCameraController _cameraController = null!;

    private readonly List<ICameraScheme> _cameraSchemes = [new WasdMovement(keyboardManager)];

    public Camera<IsometricView, OrthographicProjection> Camera => _cameraController.Camera;

    private Matrix4X4<float> _viewMatrix;
    private Matrix4X4<float> _rotationMatrix;
    private Matrix4X4<float> _projectionMatrix;
    private Vector3D<float> _cameraViewDirection;

    public Matrix4X4<float> ViewMatrix => _viewMatrix;
    public Matrix4X4<float> ProjectionMatrix => _projectionMatrix;

    public Result Load()
    {
        _cameraController = IsometricOrthographicCameraController.Create(
            new Vector2D<float>(windowProvider.Value.Size.X, windowProvider.Value.Size.Y),
            new Vector3D<float>(0, 0, 0),
            new Vector3D<float>(0.701f, -1, 0.701f),
            40f);

        screenResizedEvent.OnWindowResize.Subscribe(OnWindowResize);
        return Result.Success();
    }

    public Result Unload()
    {
        screenResizedEvent.OnWindowResize.Unsubscribe(OnWindowResize);
        return Result.Success();
    }

    CameraMovementInput _movementInput;

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        _movementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _movementInput += scheme.GetMovementInput();
        }

        return Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        _cameraController.Move(_movementInput.Direction, deltaTime);
        _cameraController.Zoom(_movementInput.Zoom, deltaTime);

        _viewMatrix = Camera.GetViewMatrix();
        _projectionMatrix = Camera.GetProjectionMatrix();

        _rotationMatrix = _viewMatrix.ExtractRotation();

        _cameraViewDirection = Vector3D.Transform(Vector3D<float>.UnitZ, _rotationMatrix);

        _movementInput = new CameraMovementInput();

        return Result.Success();
    }

    public void ApplyCameraPositionParameters(ICameraPositionShader positionShader)
    {
        positionShader.View = _viewMatrix;
        positionShader.Projection = _projectionMatrix;
    }

    public void ApplyCameraDirectionParameters(ICameraDirectionShader shader)
    {
        shader.CameraDirection = _cameraViewDirection;
    }

    /// <summary>
    /// Sets the camera to look at a target position with a specified zoom level.
    /// </summary>
    /// <param name="target">The world position to center on</param>
    /// <param name="tilesAcross">Number of tiles visible across the X axis (controls zoom)</param>
    public void SetCameraPosition(Vector3D<float> target, float tilesAcross)
    {
        _cameraController.Camera.View.Position = target + Vector3D.Normalize(new Vector3D<float>(0.701f, -1, 0.701f));
        _cameraController.Camera.Projection.OrthographicSize = tilesAcross;
    }

    private void OnWindowResize(Vector2D<int> newWindowSize)
    {
        var aspectRatio = (float)newWindowSize.X / newWindowSize.Y;
        _cameraController.Camera.Projection.AspectRatio = aspectRatio;
    }
}