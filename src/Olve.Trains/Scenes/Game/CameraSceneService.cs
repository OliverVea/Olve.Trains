using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Game;

public class CameraSceneService(Provider<IWindow> windowProvider, KeyboardManager keyboardManager) : SceneService
{
    private IsometricOrthographicCameraController _cameraController = null!;

    private readonly List<ICameraScheme> _cameraSchemes = [];

    public Camera<IsometricView, OrthographicProjection> Camera => _cameraController.Camera;
    private Vector2D<float> WindowSize => new (windowProvider.Value.Size.X, windowProvider.Value.Size.Y);

    public override Result Load()
    {
        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(0.701f, -1, 0.701f);

        const float orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(WindowSize, cameraTarget, cameraViewDirection, orthographicSize);

        _cameraSchemes.Add(new WasdMovement(keyboardManager));

        return Result.Success();
    }

    CameraMovementInput _movementInput;

    public override Result<Pass> Input(TimeSpan deltaTime)
    {
        _movementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _movementInput += scheme.GetMovementInput();
        }

        return Pass.Pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        _cameraController.Move(_movementInput.Direction, deltaTime);
        _cameraController.Zoom(_movementInput.Zoom, deltaTime);

        _movementInput = new CameraMovementInput();

        return Result.Success();
    }
}