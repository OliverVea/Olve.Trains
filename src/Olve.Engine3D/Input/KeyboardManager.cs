using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Olve.Engine3D.Input;

public class KeyboardManager
{
    private readonly ConcurrentDictionary<Keys, HashSet<Action<KeyStateChange>>> _keyStateChangedHandlers = [];

    private HashSet<Keys> _lastKeysDown = [];

    public void Update(GameTime gameTime)
    {
        var state = Keyboard.GetState();
        var keysDown = state.GetPressedKeys().ToHashSet();

        foreach (var key in _keyStateChangedHandlers.Keys)
        {
            var before = _lastKeysDown.Contains(key);
            var after = keysDown.Contains(key);

            var keyState = after ? KeyState.Down : KeyState.Up;
            InvokeKeyEvent(key, keyState);

            if (before == after)
            {
                continue;
            }

            keyState = after ? KeyState.Pressed : KeyState.Released;
            InvokeKeyEvent(key, keyState);
        }

        _lastKeysDown = keysDown;
    }

    public void Subscribe(Keys key, Action<KeyStateChange> action)
    {
        if (!_keyStateChangedHandlers.TryGetValue(key, out var actions))
        {
            actions = [];
            _keyStateChangedHandlers[key] = actions;
        }

        actions.Add(action);
    }

    public void Unsubscribe(Keys key, Action<KeyStateChange> action)
    {
        if (!_keyStateChangedHandlers.TryGetValue(key, out var actions))
        {
            return;
        }

        if (!actions.Remove(action))
        {
            return;
        }

        if (actions.Count == 0)
        {
            _keyStateChangedHandlers.Remove(key, out _);
        }
    }

    private void InvokeKeyEvent(Keys key, KeyState keyState)
    {
        if (!_keyStateChangedHandlers.TryGetValue(key, out var actions))
        {
            return;
        }

        foreach (var action in actions)
        {
            action.Invoke(new KeyStateChange(key, keyState));
        }
    }

}