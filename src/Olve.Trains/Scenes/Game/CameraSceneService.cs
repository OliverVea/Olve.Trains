using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class CameraSceneService : ISceneService
{
    private IsometricOrthographicCameraController _cameraController = null!;

    private readonly List<ICameraScheme> _cameraSchemes = [];

    public Camera<IsometricView, OrthographicProjection> Camera => _cameraController.Camera;

    public Result Load()
    {
        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(0.701f, -1, 0.701f);

        const float orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(cameraTarget, cameraViewDirection, orthographicSize);

        _cameraSchemes.Add(new WasdMovement());

        return Result.Success();
    }

    CameraMovementInput _movementInput;

    public Result<Pass> Input()
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

        _movementInput = new CameraMovementInput();

        return Result.Success();
    }

    public Result Unload()
    {
        return Result.Success();
    }
}