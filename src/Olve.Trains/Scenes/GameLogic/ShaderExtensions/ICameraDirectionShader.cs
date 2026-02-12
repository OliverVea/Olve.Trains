namespace Olve.Trains.Scenes.GameLogic.ShaderExtensions;

public interface ICameraDirectionShader
{
    Vector3D<float>? CameraDirection { get; set; }
}