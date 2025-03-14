using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Camera.Views;

public abstract class ViewBase : IView
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public abstract Matrix GetViewMatrix();
}