namespace Olve.Engine3D.Systems;

public class Event
{
    private Action? _handlers;

    public void Invoke() => _handlers?.Invoke();

    public void Subscribe(Action handler) => _handlers += handler;
    public void Unsubscribe(Action handler) => _handlers -= handler;
}

public class Event<T>
{
    private Action<T>? _handlers;

    public void Invoke(T message) =>  _handlers?.Invoke(message);

    public void Subscribe(Action<T> handlers) => _handlers += handlers;
    public void Unsubscribe(Action<T> handlers) => _handlers -= handlers;
}