namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackValidationService(TrackSplineService trackSplineService)
{
    public const int SamplingPoints = 25;
    public const float MaxCurvature = 1f;

    public bool IsValid(TrackEndpoint from, TrackEndpoint to) =>
        !trackSplineService
            .CreateSpline(from, to)
            .GetCurvatures(SamplingPoints)
            .Map(x => !x.Any(IsInvalidCurvature))
            .TryPickProblems(out _, out var value) && value;

    private static bool IsInvalidCurvature(float curvature)
    {
        return float.Abs(curvature) > MaxCurvature;
    }
}