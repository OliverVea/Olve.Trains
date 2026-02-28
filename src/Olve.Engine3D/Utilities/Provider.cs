namespace Olve.Engine3D.Utilities;

public sealed class Provider<T>(T? initialValue = default) : IDisposable
{
    private T? _value = initialValue;

    public T Value => _value ?? throw new NotInitializedException<T>();

    public void Set(T value)
    {
        _value = value;
    }

    public void Dispose()
    {
        if (_value is IDisposable disposable)
        {
            disposable.Dispose();
            _value = default;
        }
    }
}