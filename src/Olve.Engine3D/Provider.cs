using Olve.Engine3D.Utilities;

namespace Olve.Engine3D;

public sealed class Provider<T>(T? initialValue = default)
{
    private T? _value = initialValue;

    public T Value => _value ?? throw new NotInitializedException<T>();

    public void Set(T value)
    {
        _value = value;
    }
}