using Microsoft.Xna.Framework;

namespace Olve.Engine3D;

public class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}