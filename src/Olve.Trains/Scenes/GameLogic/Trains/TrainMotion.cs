using System.Runtime.InteropServices;

namespace Olve.Trains.Scenes.GameLogic.Trains;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrainMotion(float Speed, float TargetSpeed, float UserTargetSpeed, float Acceleration = 0f)
{
    public static TrainMotion Stopped { get; } = new(0f, 0f, 0f, LocomotiveProperties.Acceleration);
}
