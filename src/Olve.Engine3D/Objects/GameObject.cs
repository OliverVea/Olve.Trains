using Microsoft.Xna.Framework.Graphics;

namespace Olve.Engine3D.Objects;

public class GameObject
{
    public Transform Transform { get; set; } = new();

    public required Model Model { get; set; }
}