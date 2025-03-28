using Olve.Engine3D.Camera;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes;

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

    public Result Update(TimeSpan deltaTime)
    {
        var movementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            movementInput += scheme.GetMovementInput();
        }

        _cameraController.Move(movementInput.Direction, deltaTime);
        _cameraController.Zoom(movementInput.Zoom, deltaTime);

        return Result.Success();
    }

    public Result Unload() => throw new NotImplementedException();
}