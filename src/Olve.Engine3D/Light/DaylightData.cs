namespace Olve.Engine3D.Light;

public class DaylightData
{
    public required Curve<float> Angle { get; set; }
    public required Curve<Vector3D<float>> Color { get; set; }
    public required Curve<float> Intensity { get; set; }

    public required Curve<Vector3D<float>> AmbientColor { get; set; }
    public required Curve<float> AmbientIntensity { get; set; }
}