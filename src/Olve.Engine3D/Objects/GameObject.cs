using Microsoft.Xna.Framework.Graphics;
using Olve.Engine3D.Core;

namespace Olve.Engine3D.Objects;

public class GameObject
{
    public Transform Transform { get; set; } = new();

    public required Model Model { get; set; }
}