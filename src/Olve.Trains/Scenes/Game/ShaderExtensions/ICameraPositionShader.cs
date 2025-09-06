namespace Olve.Trains.Scenes.Game.ShaderExtensions;

public interface ICameraPositionShader
{
    Matrix4X4<float> View { get; set; }
    Matrix4X4<float> Projection { get; set; }
}