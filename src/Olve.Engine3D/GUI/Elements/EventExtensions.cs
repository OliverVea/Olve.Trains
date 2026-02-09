using Olve.Engine3D.Systems;

namespace Olve.Engine3D.GUI.Elements;

public static class EventExtensions
{
    public static void Invoke<T1, T2, T3>(this Event<(T1, T2, T3)> e, T1 arg1, T2 arg2, T3 arg3)
    {
        e.Invoke((arg1, arg2, arg3));
    }
}