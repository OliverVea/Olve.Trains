namespace Olve.Engine3D.Light;

public class Curve1(InterpolationType interpolationType, params IEnumerable<DayTimeKeyFrame<float>> keyFrames) : Curve<float>(interpolationType, keyFrames);