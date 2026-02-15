using Olve.Engine3D.Systems;

namespace Olve.Engine3D;

public class AfterRenderEvent
{
    public Event OnAfterRender { get; } = new();
}
