namespace Olve.Engine3D.Rendering.Entities;

/// <summary>
/// Represents a texture slot index in OpenGL (0-31 on most hardware, though typically only 16+ are guaranteed).
/// </summary>
public readonly record struct Slot(byte Value)
{
    /// <summary>
    /// Default texture slot (Texture0) - used for most UI textures and images.
    /// </summary>
    public static readonly Slot Default = new(0);

    /// <summary>
    /// Font atlas texture slot (Texture1) - reserved for text rendering.
    /// </summary>
    public static readonly Slot FontAtlas = new(1);

    /// <summary>
    /// Maximum supported slot value (31 - hardware typically supports 32 units).
    /// </summary>
    public const byte MaxValue = 31;

    public static implicit operator byte(Slot slot) => slot.Value;
    public static explicit operator Slot(byte value) => new(value);
}
