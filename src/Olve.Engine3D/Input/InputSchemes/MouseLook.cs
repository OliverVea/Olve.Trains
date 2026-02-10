using Olve.Engine3D.Utilities;
using Silk.NET.Windowing;

namespace Olve.Engine3D.Input.InputSchemes;

public class MouseLook(MouseManager mouseManager, Provider<IWindow> windowProvider) : ICameraScheme
{
    private MouseState MouseState => mouseManager.State;

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

        rotation.X = MouseState.Delta.X / windowProvider.Value.Size.X;
        rotation.Y = -MouseState.Delta.Y / windowProvider.Value.Size.Y;

        return rotation;
    }

    private float GetZoom()
    {
        return MouseState.Scroll;
    }
}