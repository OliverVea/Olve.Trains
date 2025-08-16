using Olve.CodeGen;
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
using Olve.Results;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Olve.Trains.Scenes.Game;

public class CameraSceneService(ILoggingManager loggingManager, Provider<IWindow> windowProvider, KeyboardManager keyboardManager) : SceneService(loggingManager)
{
    private IsometricOrthographicCameraController _cameraController = null!;

    private readonly List<ICameraScheme> _cameraSchemes = [];

    public Camera<IsometricView, OrthographicProjection> Camera => _cameraController.Camera;
    private Vector2D<float> WindowSize => new (windowProvider.Value.Size.X, windowProvider.Value.Size.Y);
    
    
    public Matrix4X4<float> ViewMatrix { get; set; }
    public Matrix4X4<float> RotationMatrix { get; set; }
    public Matrix4X4<float> ProjectionMatrix { get; set; }
    
    public Vector3D<float> CameraViewDirection { get; set; }

    protected override Result OnLoad()
    {
        Vector3D<float> cameraTarget = new (0, 0, 0);
        Vector3D<float> cameraViewDirection = new(0.701f, -1, 0.701f);

        const float orthographicSize = 40f;

        _cameraController = IsometricOrthographicCameraController.Create(WindowSize, cameraTarget, cameraViewDirection, orthographicSize);

        _cameraSchemes.Add(new WasdMovement(keyboardManager));

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
        
        ViewMatrix = Camera.GetViewMatrix();
        ProjectionMatrix = Camera.GetProjectionMatrix();
        
        RotationMatrix = ViewMatrix.ExtractRotation();
        
        CameraViewDirection = Vector3D.Transform(Vector3D<float>.UnitZ, RotationMatrix);
        
        _movementInput = new CameraMovementInput();

        return Result.Success();
    }

    public void ApplyCameraPositionParameters(ICameraPositionShader positionShader)
    {
        positionShader.View = ViewMatrix;
        positionShader.Projection = ProjectionMatrix;
    }

    public void ApplyCameraDirectionParameters(ICameraDirectionShader shader)
    {
        shader.CameraDirection = CameraViewDirection;
    }
}