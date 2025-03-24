using Silk.NET.Input;

namespace Olve.Engine3D.Input;

public class KeyboardManager
{
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<Key> _releasedKeys = [];

    public KeyboardState State { get; private set; } = new();

    public Result Initialize()
    {
        foreach (var keyboard in GameManager.Input.Keyboards)
        {
            keyboard.KeyDown += OnKeyPressed;
            keyboard.KeyUp += OnKeyReleased;
        }

        return Result.Success();
    }

    public Result Input(TimeSpan _)
    {
        State.Set(_pressedKeys, _releasedKeys);

        _pressedKeys.Clear();
        _releasedKeys.Clear();

        return Result.Success();
    }

    private void OnKeyPressed(IKeyboard keyboard, Key key, int arg3)
    {
        _pressedKeys.Add(key);
    }

    private void OnKeyReleased(IKeyboard keyboard, Key key, int arg3)
    {
        _releasedKeys.Add(key);
    }
}