using Microsoft.Xna.Framework;

namespace Olve.Engine3D.Graphics;

public interface IDrawable
{
    void Initialize();
        
    void Draw(Matrix view, Matrix projection);
}