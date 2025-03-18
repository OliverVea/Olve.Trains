namespace Olve.Engine3D.Input.InputSchemes;

public class MouseLook : ICameraScheme
{
    private MouseState MouseState => GameManager.MouseManager.State;

    public CameraMovementInput GetMovementInput()
    {
        return new CameraMovementInput
        {
            Rotation = GetRotation(),
            Zoom = GetZoom()
        };
    }

    private Vector2D<float> GetRotation()
    {
        var rotation = Vector2D<float>.Zero;

        rotation.X = MouseState.Delta.X / GameManager.Window.Size.X;
        rotation.Y = -MouseState.Delta.Y / GameManager.Window.Size.Y;

        return rotation;
    }

    private float GetZoom()
    {
        return MouseState.Scroll;
    }
}