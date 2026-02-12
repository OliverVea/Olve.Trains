using Olve.Engine3D.Light;
using Olve.Engine3D.Time;
using Curve1 = Olve.Engine3D.Light.Curve<float>;
using Curve3 = Olve.Engine3D.Light.Curve<Silk.NET.Maths.Vector3D<float>>;

namespace Olve.Trains.Scenes.GameLogic.Light;

public static class LightingConstants
{
    public static readonly DayTime Midnight0 = new(0);
    public static readonly DayTime Midnight24 = new(24);
    public static readonly DayTime BeforeSunrise = new(5, 30);
    public static readonly DayTime SunriseStart = new(6);
    public static readonly DayTime SunriseMiddle = new(7, 30);
    public static readonly DayTime SunriseEnd = new(9);
    public static readonly DayTime Noon = new(13, 30);
    public static readonly DayTime SunsetBegin = new(18);
    public static readonly DayTime SunsetMiddle = new(19, 30);
    public static readonly DayTime SunsetEnd = new(21);
    public static readonly DayTime AfterSunset = new(21, 30);

    public static class Colors
    {
        public static readonly Vector3D<float> Black = new(0, 0, 0);
        public static readonly Vector3D<float> White = new Vector3D<float>(255,255,255) / 255f;

        public static readonly Vector3D<float> Shadow = new Vector3D<float>(91,91,111) / 255f;
        public static readonly Vector3D<float> SunRed = new Vector3D<float>(231,76,60) / 255f;
        public static readonly Vector3D<float> SunOrange = new Vector3D<float>(243,156,18) / 255f;
        public static readonly Vector3D<float> SunPaleOrange = new Vector3D<float>(245,176,65) / 255f;
        public static readonly Vector3D<float> SunYellow = new Vector3D<float>(247,220,111) / 255f;
        public static readonly Vector3D<float> SunWhite = (White + SunYellow) * 0.5f;

        public static readonly Vector3D<float> MoonBlue = new Vector3D<float>(110, 170, 255) / 255f;
        public static readonly Vector3D<float> MoonWhite = new Vector3D<float>(200, 200, 255) / 255f;
    }

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] SunColor =
    [
        (Midnight0, Colors.Black),
        (BeforeSunrise, Colors.Black),
        (SunriseStart, Colors.SunRed),
        (SunriseMiddle, Colors.SunOrange),
        (SunriseEnd, Colors.SunWhite),
        (SunsetBegin, Colors.SunWhite),
        (SunsetMiddle, Colors.SunPaleOrange),
        (SunsetEnd, Colors.SunRed),
        (AfterSunset, Colors.Black),
        (Midnight24, Colors.Black),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunAngle =
    [
        (Midnight0, 0),
        (SunriseStart, 0),
        (SunsetMiddle, 180),
        (Midnight24, 180),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunIntensity =
    [
        (Midnight0, 0),
        (BeforeSunrise, 0),
        (SunriseEnd, 0.85f),
        (Noon, 1f),
        (SunsetBegin, 0.85f),
        (AfterSunset, 0),
        (Midnight24, 0),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunAmbientIntensity =
    [
        (Midnight0, 0),
        (BeforeSunrise, 0),
        (SunriseEnd, 0.45f),
        (Noon, 0.6f),
        (SunsetBegin, 0.45f),
        (AfterSunset, 0),
        (Midnight24, 0),
    ];

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] SunAmbientColor =
    [
        (Midnight0, Colors.Black),
        (Midnight24, Colors.Black),
    ];

    public static readonly DaylightData SunData = new()
    {
        Angle = new Curve1(InterpolationType.Linear, SunAngle) { Min = 0, Max = 180},
        Color = new Curve3(InterpolationType.CatmullRom, SunColor) { Min = Colors.Black, Max = Colors.White},
        Intensity = new Curve1(InterpolationType.CatmullRom, SunIntensity) { Min = 0, Max = 1},
        AmbientColor = new Curve3(InterpolationType.Linear, SunAmbientColor),
        AmbientIntensity = new Curve1(InterpolationType.CatmullRom, SunAmbientIntensity) { Min = 0, Max = 1},
    };

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] MoonColor =
    [
        (Midnight0, Colors.MoonWhite),
        (Midnight24, Colors.MoonWhite),
    ];

    private static readonly DayTimeKeyFrame<float>[] MoonAngle = SunAngle.Select(x => x with { Value = (180 + x.Value) % 360 }).ToArray();

    private static readonly DayTimeKeyFrame<float>[] MoonIntensity =
    [
        (new DayTime(0), 0.45f),
        (new DayTime(5), 0.45f),
        (new DayTime(7), 0),
        (new DayTime(17), 0),
        (new DayTime(21), 0.45f),
        (new DayTime(24), 0.45f),
    ];

    private static readonly DayTimeKeyFrame<float>[] MoonAmbientIntensity =
    [
        (Midnight0, 0.25f),
        (Midnight24, 0.25f),
    ];

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] MoonAmbientColor =
    [
        (Midnight0, Colors.MoonBlue),
        (Midnight24, Colors.MoonBlue),
    ];

    public static readonly DaylightData MoonData = new()
    {
        Angle = new Curve1(InterpolationType.Linear, MoonAngle) { Min = 0, Max = 180 },
        Color = new Curve3(InterpolationType.CatmullRom, MoonColor) { Min = Colors.Black, Max = Colors.MoonWhite },
        Intensity = new Curve1(InterpolationType.CatmullRom, MoonIntensity) { Min = 0, Max = 1f },
        AmbientColor = new Curve3(InterpolationType.Linear, MoonAmbientColor),
        AmbientIntensity = new Curve1(InterpolationType.CatmullRom, MoonAmbientIntensity) { Min = 0, Max = 1f },
    };
}