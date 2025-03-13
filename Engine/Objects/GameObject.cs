using Microsoft.Xna.Framework.Graphics;

namespace Engine.Objects;

public class GameObject
{
    public Transform Transform { get; set; } = new();

    public required Model Model { get; set; }
}