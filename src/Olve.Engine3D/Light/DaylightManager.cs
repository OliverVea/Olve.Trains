using Olve.Engine3D.Math.Splines;

namespace Olve.Engine3D.Light;

public class DaylightManager
{
    private readonly Dictionary<DaylightId, DaylightInterpolator> _lights = [];

    public Result<DaylightId> AddLight(DaylightData data)
    {
        Result[] results =
        [
            data.Angle.Validate().IfProblem(p => p.Prepend("Direction is invalid")),
            data.Color.Validate().IfProblem(p => p.Prepend("Color is invalid")),
            data.Intensity.Validate().IfProblem(p => p.Prepend("Intensity is invalid")),
            data.AmbientColor.Validate().IfProblem(p => p.Prepend("AmbientColor is invalid")),
            data.AmbientIntensity.Validate().IfProblem(p => p.Prepend("AmbientIntensity is invalid"))
        ];

        if (results.TryPickProblems(out var problem))
        {
            return problem.Prepend("Light is invalid.");
        }

        var interpolator = CreateInterpolator(data);
        var daylightId = new DaylightId(_lights.Count);

        _lights.Add(daylightId, interpolator);

        return daylightId;
    }

    public DeletionResult RemoveLight(DaylightId daylightId)
    {
        if (!_lights.Remove(daylightId))
        {
            DeletionResult.NotFound();
        }

        return DeletionResult.Success();
    }

    public Result<DaylightValue?> Sample(DaylightId daylightId, DayTime time)
    {
        if (!_lights.TryGetValue(daylightId, out var data))
        {
            return new ResultProblem("Light with id '{0}' does not exist.", daylightId.Value);
        }

        return new DaylightValue
        {
            Angle = data.AngleInterpolator.Sample(time.Value),
            Color = data.ColorInterpolator.Sample(time.Value),
            Intensity = data.IntensityInterpolator.Sample(time.Value),
            AmbientColor = data.AmbientColorInterpolator.Sample(time.Value),
            AmbientIntensity = data.AmbientIntensityInterpolator.Sample(time.Value)
        };
    }

    private DaylightInterpolator CreateInterpolator(DaylightData daylightData)
    {
        return new DaylightInterpolator
        {
            AngleInterpolator = CreateInterpolator(daylightData.Angle),
            ColorInterpolator = CreateInterpolator(daylightData.Color),
            IntensityInterpolator = CreateInterpolator(daylightData.Intensity),
            AmbientColorInterpolator = CreateInterpolator(daylightData.AmbientColor),
            AmbientIntensityInterpolator = CreateInterpolator(daylightData.AmbientIntensity)
        };
    }

    private IInterpolator<float> CreateInterpolator(Curve<float> curve)
    {
        IInterpolator<float> interpolator = curve.InterpolationType switch
        {
            InterpolationType.None => new NoneInterpolator<float>(0f),
            InterpolationType.Linear => new LinearInterpolator1(curve.KeyFrames),
            InterpolationType.CatmullRom => new CatmullRom1(curve.KeyFrames),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (curve.Min.HasValue)
        {
            interpolator.Min = curve.Min.Value;
        }

        if (curve.Max.HasValue)
        {
            interpolator.Max = curve.Max.Value;
        }

        return interpolator;
    }

    private CatmullRom3 CreateInterpolator(Curve<Vector3D<float>> curve)
    {
        var catmullRom = new CatmullRom3(curve.KeyFrames);

        if (curve.Min.HasValue)
        {
            catmullRom.Min = curve.Min.Value;
        }

        if (curve.Max.HasValue)
        {
            catmullRom.Max = curve.Max.Value;
        }

        return catmullRom;
    }


    private class DaylightInterpolator
    {
        public required IInterpolator<float> AngleInterpolator { get; set; }
        public required IInterpolator<Vector3D<float>> ColorInterpolator { get; set; }
        public required IInterpolator<float> IntensityInterpolator { get; set; }

        public required IInterpolator<Vector3D<float>> AmbientColorInterpolator { get; set; }
        public required IInterpolator<float> AmbientIntensityInterpolator { get; set; }
    }
}