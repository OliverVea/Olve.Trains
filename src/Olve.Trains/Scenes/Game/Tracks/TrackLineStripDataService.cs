using Olve.Engine3D.Rendering.Entities;

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

    public LineStripData GetLineStripData(TrackEndpoint start, TrackEndpoint end)
    {
        var positions = trackSplineService.GetPoints(start, end, TrackVertexCount);
        return BuildLineStripData(positions);
    }

    private static LineStripData BuildLineStripData(Vector3D<float>[] positions)
    {
        var colors = new Vector3D<float>[positions.Length];
        Array.Fill(colors, new Vector3D<float>(1, 1, 1));

        return new LineStripData
        {
            Positions = positions,
            Colors = colors,
        };
    }
}
