using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Engine3D.Input;

public class MouseManager(Provider<IInputContext> inputContextProvider, Provider<IWindow> windowProvider)
{
    private float _scroll;
    private readonly HashSet<MouseButton> _pressedButtons = [];
    private readonly HashSet<MouseButton> _releasedButtons = [];

    public MouseState State { get; } = new();

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

        var position = Vector2D<float>.Zero;

        foreach (var mouse in inputContextProvider.Value.Mice)
        {
            var pos = mouse.Position;
            position += new Vector2D<float>(pos.X, pos.Y);
        }

        State.Delta = position - State.Position;
        State.Position = position;
        State.NormalizedPosition = new Vector2D<float>(
            position.X / windowProvider.Value.Size.X - 0.5f,
            position.Y / windowProvider.Value.Size.Y - 0.5f
        ) * 2f;
        State.Scroll = _scroll;

        return Result.Success();
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