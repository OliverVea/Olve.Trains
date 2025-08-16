using Olve.CodeGen;
using Olve.Engine3D.Light;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class SceneLightService(ILoggingManager loggingManager, DayTimeManager dayTimeManager, DaylightManager daylightManager) : SceneService(loggingManager)
{
    private static readonly DayTime DayStart = new(5, 30);
    private static readonly TimeSpan DayDuration = TimeSpan.FromMinutes(6);
    
    private DaylightId _sunId, _moonId;
    
    public Vector3D<float> SunDirection { get; set; }
    public DaylightValue SunValue { get; set; } = new();
    public Vector3D<float> MoonDirection { get; set; }
    public DaylightValue MoonValue { get; set; } = new();
    
    public Vector3D<float> AmbientLightColor => SunValue.AmbientColor * SunValue.AmbientIntensity + MoonValue.AmbientColor * MoonValue.AmbientIntensity;

    protected override Result OnLoad()
    {
        dayTimeManager.CurrentTime = DayStart;
        dayTimeManager.DayLength = DayDuration;

        if (daylightManager.AddLight(GameSceneEntities.SunData).TryPickProblems(out var problems, out _sunId))
        {
            return problems;
        }

        if (daylightManager.AddLight(GameSceneEntities.MoonData).TryPickProblems(out problems, out _moonId))
        {
            return problems;
        }

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        dayTimeManager.Step(deltaTime);

        if (daylightManager.Sample(_sunId, dayTimeManager.CurrentTime)
            .TryPickProblems(out var problems, out var sunValue))
        {
            return problems;
        }

        if (daylightManager.Sample(_moonId, dayTimeManager.CurrentTime)
            .TryPickProblems(out problems, out var moonValue))
        {
            return problems;
        }
        
        var sunRadians = float.DegreesToRadians(-sunValue.Angle);
        var sunDirection = Vector3D.Transform(Vector3D<float>.UnitX, Matrix4X4.CreateRotationZ(sunRadians));

        var moonRadians = float.DegreesToRadians(-moonValue.Angle);
        var moonDirection = Vector3D.Transform(Vector3D<float>.UnitX, Matrix4X4.CreateRotationZ(moonRadians));
        
        SunValue = sunValue;
        MoonValue = moonValue;
        SunDirection = sunDirection;
        MoonDirection = moonDirection;

        return Result.Success();
    }

    protected override Result OnUnload()
    {
        daylightManager.RemoveLight(_sunId);
        daylightManager.RemoveLight(_moonId);

        return Result.Success();
    }

    public void ApplyShaderParameters(IDaylightShader shader)
    {
        shader.DirectionalLight0Dir = SunDirection;
        shader.DirectionalLight0Color = SunValue.Color;
        shader.DirectionalLight0Intensity = SunValue.Intensity;

        shader.DirectionalLight1Dir = SunDirection;
        shader.DirectionalLight1Color = MoonValue.Color;
        shader.DirectionalLight1Intensity = MoonValue.Intensity;

        shader.AmbientLightColor = AmbientLightColor;
        shader.AmbientLightIntensity = 1f;
        
    }
}