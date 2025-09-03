namespace Olve.Engine3D.Systems;

public class Event<T>
{
    private Action<T>? _handlers;
    
    public void Invoke(T message) =>  _handlers?.Invoke(message);
    
    public void Subscribe(Action<T> handlers) => _handlers += handlers;
    public void Unsubscribe(Action<T> handlers) => _handlers -= handlers;
}