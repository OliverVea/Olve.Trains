namespace Olve.Engine3D.Light;

public class Curve3(InterpolationType interpolationType, params IEnumerable<DayTimeKeyFrame<Vector3D<float>>> keyFrames) : Curve<Vector3D<float>>(interpolationType, keyFrames);