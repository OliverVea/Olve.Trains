using Silk.NET.Input;

namespace Olve.Engine3D.Input;

public class MouseState
{
    private readonly HashSet<MouseButton> _downButtons = [];
    private readonly HashSet<MouseButton> _pressedButtons = [];
    private readonly HashSet<MouseButton> _releasedButtons = [];

    public bool IsButtonDown(MouseButton button) => _downButtons.Contains(button);
    public bool IsButtonPressed(MouseButton button) => _pressedButtons.Contains(button);
    public bool IsButtonReleased(MouseButton button) => _releasedButtons.Contains(button);

    public Vector2D<float> Position { get; set; }
    public Vector2D<float> Delta { get; set; }
    public float Scroll { get; set; }

    public void Set(IReadOnlySet<MouseButton> pressedButtons, IReadOnlySet<MouseButton> releasedButtons)
    {
        _pressedButtons.Clear();
        _releasedButtons.Clear();

        foreach (var button in pressedButtons)
        {
            _pressedButtons.Add(button);
            _downButtons.Add(button);
        }

        foreach (var button in releasedButtons)
        {
            _releasedButtons.Add(button);
            _downButtons.Remove(button);
        }
    }
}