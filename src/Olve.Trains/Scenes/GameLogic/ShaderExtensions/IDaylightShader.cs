namespace Olve.Trains.Scenes.Game.ShaderExtensions;

public interface IDaylightShader
{
    Vector3D<float>? DirectionalLight0Dir { get; set; }
    Vector3D<float>? DirectionalLight0Color { get; set; }
    float? DirectionalLight0Intensity { get; set; }
    Vector3D<float>? DirectionalLight1Dir { get; set; }
    Vector3D<float>? DirectionalLight1Color { get; set; }
    float? DirectionalLight1Intensity { get; set; }
    Vector3D<float>? AmbientLightColor { get; set; }
    float? AmbientLightIntensity { get; set; }
}