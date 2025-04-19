using System.Diagnostics.CodeAnalysis;
using Silk.NET.Input;

namespace Olve.Engine3D.Input;

public class KeyboardState
{
    private readonly HashSet<Key> _downKeys = [];
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<Key> _releasedKeys = [];

    public bool IsKeyDown(Key key) => _downKeys.Contains(key);
    public bool IsKeyPressed(Key key) => _pressedKeys.Contains(key);

    public bool TryGetPressed(IEnumerable<Key> keys, [NotNullWhen(true)] out Key? pressedKey)
    {
        foreach (var key in keys)
        {
            if (IsKeyPressed(key))
            {
                pressedKey = key;
                return true;
            }
        }

        pressedKey = null;
        return false;
    }
    
    public bool IsKeyReleased(Key key) => _releasedKeys.Contains(key);

    public void Set(IReadOnlySet<Key> pressedKeys, IReadOnlySet<Key> releasedKeys)
    {
        _pressedKeys.Clear();
        _releasedKeys.Clear();

        foreach (var key in pressedKeys)
        {
            _pressedKeys.Add(key);
            _downKeys.Add(key);
        }

        foreach (var key in releasedKeys)
        {
            _releasedKeys.Add(key);
            _downKeys.Remove(key);
        }
    }
}