using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game.ShaderExtensions;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Rendering;

public class CameraSceneService(ILoggingManager loggingManager, Provider<IWindow> windowProvider, KeyboardManager keyboardManager, ScreenResizedEvent screenResizedEvent) : SceneService(loggingManager)
{
    private IsometricOrthographicCameraController _cameraController = null!;

    private readonly List<ICameraScheme> _cameraSchemes = [];

    public Camera<IsometricView, OrthographicProjection> Camera => _cameraController.Camera;
    private Vector2D<float> WindowSize => new (windowProvider.Value.Size.X, windowProvider.Value.Size.Y);


    private Matrix4X4<float> _viewMatrix;
    private Matrix4X4<float> _rotationMatrix;
    private Matrix4X4<float> _projectionMatrix;
    private Vector3D<float> _cameraViewDirection;

    protected override Result OnLoad()
    {
        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(0.701f, -1, 0.701f);

        const float orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(WindowSize, cameraTarget, cameraViewDirection, orthographicSize);

        _cameraSchemes.Add(new WasdMovement(keyboardManager));
        screenResizedEvent.OnWindowResize.Subscribe(OnWindowResize);

        return Result.Success();
    }

    protected override Result OnUnload()
    {
        screenResizedEvent.OnWindowResize.Unsubscribe(OnWindowResize);
        return Result.Success();
    }

    CameraMovementInput _movementInput;

    protected override Result<Pass> OnInput(TimeSpan deltaTime)
    {
        _movementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _movementInput += scheme.GetMovementInput();
        }

        return Pass.Pass;
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
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

    private void OnWindowResize(Vector2D<int> newWindowSize)
    {
        var aspectRatio = (float)newWindowSize.X / newWindowSize.Y;
        _cameraController.Camera.Projection.AspectRatio = aspectRatio;
    }
}