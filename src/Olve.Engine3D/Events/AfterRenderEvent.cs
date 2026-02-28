using Olve.Engine3D.Systems;

namespace Olve.Engine3D.Events;

public class AfterRenderEvent
{
    public Event AfterRender { get; } = new();
}