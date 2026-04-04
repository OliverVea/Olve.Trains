namespace Olve.Trains.Scenes.GameLogic.Trains;

/// <summary>
/// Physical properties of the default locomotive (EMD SD70ACe).
/// All values in game units (1 game unit = <see cref="WorldScale.TileSizeInMeters"/> meters).
/// See docs/specifications/freight-train.md for real-world sources.
/// </summary>
public static class LocomotiveProperties
{
    private const float M = WorldScale.TileSizeInMeters;

    public static readonly float Length = 22.4f / M;
    public static readonly float Width = 3.2f / M;
    public static readonly float Height = 4.7f / M;
    public static readonly float MaxSpeed = 28f / M;
    public static readonly float CouplingGap = 1.1f / M;

    // Mass in kg
    public static readonly float Mass = 188_000f;

    // Tractive effort: constant force below corner speed, constant power above
    public static readonly float MaxTractiveEffort = 698_000f;     // N (at standstill, adhesion-limited)
    public static readonly float EffectivePower = 2_750_000f;      // W (after drivetrain losses)
    public static readonly float CornerSpeed = EffectivePower / MaxTractiveEffort; // ~3.9 m/s

    // Derived: acceleration at v=0 (F_max / mass), converted to game units
    public static readonly float Acceleration = MaxTractiveEffort / Mass / M;

    public static readonly float ServiceBrakingDeceleration = 0.2f / M;
    public static readonly float EmergencyBrakingDeceleration = 0.5f / M;
}
