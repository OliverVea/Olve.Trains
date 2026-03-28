using System.Runtime.InteropServices;

namespace Olve.Trains.Scenes.GameLogic.Trains;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrainMotion(float Speed, float TargetSpeed, float Acceleration = 1f);
