using Olve.Engine3D.Assets.Entities;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackLineStripDataService(TrackSplineService trackSplineService)
{
    private const int TrackVertexCount = 100;

    public Result<LineStripData> GetLineStripData(Id<Track> trackId)
    {
        if (trackSplineService.GetPoints(trackId, TrackVertexCount).TryPickProblems(out var problems, out var positions))
        {
            return problems.Prepend("Failed to get track points");
        }

        return BuildLineStripData(positions);
    }

    public Result<LineStripData> GetLineStripData(TrackEndpoint start, TrackEndpoint end)
    {
        var spline = trackSplineService.CreateSpline(start, end);
        if (spline
            .GetPoints(TrackVertexCount)
            .TryPickProblems(out var problems, out var positions))
        {
            return problems;
        }

        return BuildLineStripData(positions);
    }

    private static LineStripData BuildLineStripData(IEnumerable<Vector3D<float>> positions)
    {
        var positionArray = positions as Vector3D<float>[] ?? positions.ToArray();

            var colors = new Vector3D<float>[positionArray.Length];
        Array.Fill(colors, new Vector3D<float>(1, 1, 1));

        return new LineStripData
        {
            Positions = positionArray,
            Colors = colors,
        };
    }
}
