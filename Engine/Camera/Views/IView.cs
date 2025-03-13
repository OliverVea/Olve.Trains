using Microsoft.Xna.Framework;

namespace Engine.Camera.Views;

public interface IView
{
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }

    Matrix GetViewMatrix();
}