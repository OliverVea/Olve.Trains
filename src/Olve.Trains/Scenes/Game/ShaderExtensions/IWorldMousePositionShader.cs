using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.ShaderExtensions;

public interface IWorldMousePositionShader
{
    Vector3D<float> MousePosition { get; set; }
}