namespace Olve.Engine3D;

public sealed class Provider<T>
{
    private T? _value;

    public T Value => _value ?? throw new NotInitializedException<T>();

    public void Set(T value)
    {
        _value = value;
    }
}