using Olve.Engine3D.Systems;

namespace Olve.Engine3D.Events;

public class GameClosingEvent
{
    public Event GameClosing { get; } = new();
}