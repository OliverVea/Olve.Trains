namespace Olve.Engine3D.Light;

public class DaylightValue
{
    public float Angle { get; set; }
    public Vector3D<float> Color { get; set; }
    public float Intensity { get; set; }

    public Vector3D<float> AmbientColor { get; set; }
    public float AmbientIntensity { get; set; }

    public override string ToString()
    {
        return $"Direction: {Angle}, Color: {Color}, Intensity: {Intensity}, AmbientColor: {AmbientColor}, AmbientIntensity: {AmbientIntensity}";
    }
}