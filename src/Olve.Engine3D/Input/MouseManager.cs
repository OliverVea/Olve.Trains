using Olve.Engine3D.Utilities;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Engine3D.Input;

public class MouseManager(Provider<IInputContext> inputContextProvider, Provider<IWindow> windowProvider)
{
    private float _scroll;
    private readonly HashSet<MouseButton> _pressedButtons = [];
    private readonly HashSet<MouseButton> _releasedButtons = [];
    private readonly HashSet<MouseButton> _scheduledRelease = [];
    public MouseState State { get; } = new();

    /// <summary>
    /// When set, overrides the normalized mouse position directly (range -1 to 1).
    /// Persists until cleared.
    /// </summary>
    public Vector2D<float>? NormalizedPositionOverride { get; set; }

    public Result Initialize()
    {
        foreach (var mouse in inputContextProvider.Value.Mice)
        {
            mouse.MouseDown += OnButtonPressed;
            mouse.MouseUp += OnButtonReleased;
            mouse.Scroll += OnMouseScrolled;
        }

        return Result.Success();
    }

    private void OnMouseScrolled(IMouse mouse, ScrollWheel scrollWheel)
    {
        _scroll = scrollWheel.Y;
    }

    public Result Input(TimeSpan _)
    {
        State.Set(_pressedButtons, _releasedButtons);

        _pressedButtons.Clear();
        _releasedButtons.Clear();

        // Deliver scheduled releases (from SimulateClick) on the NEXT frame
        var clearOverrideAfterPosition = false;
        if (_scheduledRelease.Count > 0)
        {
            foreach (var button in _scheduledRelease)
            {
                _releasedButtons.Add(button);
            }
            _scheduledRelease.Clear();
            clearOverrideAfterPosition = true;
        }

        var position = Vector2D<float>.Zero;

        foreach (var mouse in inputContextProvider.Value.Mice)
        {
            var pos = mouse.Position;
            position += new Vector2D<float>(pos.X, pos.Y);
        }

        State.Delta = position - State.Position;
        State.Position = position;

        if (NormalizedPositionOverride is { } normalizedOverride)
        {
            State.NormalizedPosition = normalizedOverride;
        }
        else
        {
            State.NormalizedPosition = new Vector2D<float>(
                position.X / windowProvider.Value.Size.X - 0.5f,
                position.Y / windowProvider.Value.Size.Y - 0.5f
            ) * 2f;
        }
        State.Scroll = _scroll;

        // Clear the position override after the click cycle completes
        // (done after position computation so the release frame uses the correct position)
        if (clearOverrideAfterPosition)
        {
            NormalizedPositionOverride = null;
        }

        return Result.Success();
    }

    public void SimulateButtonPress(MouseButton button)
    {
        _pressedButtons.Add(button);
    }

    public void SimulateButtonRelease(MouseButton button)
    {
        _releasedButtons.Add(button);
    }

    /// <summary>
    /// Simulates a complete click: press now, release on the next frame.
    /// </summary>
    public void SimulateClick(MouseButton button)
    {
        _pressedButtons.Add(button);
        _scheduledRelease.Add(button);
    }

    private void OnButtonPressed(IMouse mouse, MouseButton button)
    {
        _pressedButtons.Add(button);
    }

    private void OnButtonReleased(IMouse mouse, MouseButton button)
    {
        _releasedButtons.Add(button);
    }
}