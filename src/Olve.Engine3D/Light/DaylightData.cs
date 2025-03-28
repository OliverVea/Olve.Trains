namespace Olve.Engine3D.Light;

public class DaylightData
{
    public Curve<float> Angle { get; set; }
    public Curve<Vector3D<float>> Color { get; set; }
    public Curve<float> Intensity { get; set; }

    public Curve<Vector3D<float>> AmbientColor { get; set; }
    public Curve<float> AmbientIntensity { get; set; }
}