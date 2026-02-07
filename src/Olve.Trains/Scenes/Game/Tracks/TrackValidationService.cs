namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackValidationService(TrackSplineService trackSplineService)
{
    public bool IsValid(TrackEndpoint from, TrackEndpoint to)
    {
        var spline = trackSplineService.CreateSpline(from, to);
        return true;
    }
}