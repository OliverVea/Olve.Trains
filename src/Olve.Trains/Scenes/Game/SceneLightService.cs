using Olve.Engine3D.Light;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class SceneLightService(DayTimeManager dayTimeManager, DaylightManager daylightManager) : SceneService
{
    private static readonly DayTime DayStart = new(5, 30);
    private static readonly TimeSpan DayDuration = TimeSpan.FromMinutes(6);
    
    private DaylightId _sunId, _moonId;

    public override Result Load()
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

    public override Result Update(TimeSpan deltaTime)
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


        GameSceneEntities.DefaultShader.DirectionalLight0Dir = sunDirection;
        GameSceneEntities.DefaultShader.DirectionalLight0Color = sunValue.Color;
        GameSceneEntities.DefaultShader.DirectionalLight0Intensity = sunValue.Intensity;

        GameSceneEntities.DefaultShader.DirectionalLight1Dir = moonDirection;
        GameSceneEntities.DefaultShader.DirectionalLight1Color = moonValue.Color;
        GameSceneEntities.DefaultShader.DirectionalLight1Intensity = moonValue.Intensity;

        GameSceneEntities.DefaultShader.AmbientLightColor = sunValue.AmbientColor * sunValue.Intensity + moonValue.AmbientColor * moonValue.Intensity;
        GameSceneEntities.DefaultShader.AmbientLightIntensity = 1f;


        GameSceneEntities.TerrainShader.DirectionalLight0Dir = sunDirection;
        GameSceneEntities.TerrainShader.DirectionalLight0Color = sunValue.Color;
        GameSceneEntities.TerrainShader.DirectionalLight0Intensity = sunValue.Intensity;

        GameSceneEntities.TerrainShader.DirectionalLight1Dir = moonDirection;
        GameSceneEntities.TerrainShader.DirectionalLight1Color = moonValue.Color;
        GameSceneEntities.TerrainShader.DirectionalLight1Intensity = moonValue.Intensity;

        GameSceneEntities.TerrainShader.AmbientLightColor = sunValue.AmbientColor * sunValue.Intensity + moonValue.AmbientColor * moonValue.Intensity;
        GameSceneEntities.TerrainShader.AmbientLightIntensity = 1f;

        return Result.Success();
    }

    public override Result Unload()
    {
        daylightManager.RemoveLight(_sunId);
        daylightManager.RemoveLight(_moonId);

        return Result.Success();
    }
}