using Microsoft.Xna.Framework;

namespace Engine.Camera.Controllers;

public interface ICameraController
{
    ICamera Camera { get; }

    void Update(GameTime gameTime);
}