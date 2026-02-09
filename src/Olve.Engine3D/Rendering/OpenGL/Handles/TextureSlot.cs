namespace Olve.Engine3D.Rendering.OpenGL.Handles;

/// <summary>
/// Represents a texture bound to a specific texture unit.
/// </summary>
public readonly record struct TextureSlot(int SlotIntex, Texture2D Texture);
