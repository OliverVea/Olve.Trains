using Olve.Engine3D.Systems;

namespace Olve.Engine3D.Events;

public class ScreenResizedEvent
{
    public Event<Vector2D<int>> OnWindowResize { get; } = new();
}