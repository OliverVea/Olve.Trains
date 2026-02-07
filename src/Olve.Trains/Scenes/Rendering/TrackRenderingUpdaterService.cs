using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Rendering;

public class TrackRenderingUpdaterService(
    ILoggingManager loggingManager,
    TrackService trackService,
    TrackSplineService trackSplineService,
    TrackRenderingService trackRenderingService) : SceneService(loggingManager)
{
    private const int TrackVertexCount = 100;

    private readonly EventQueue<Id<Track>> _onTrackAddedQueue = new(trackService.OnAdded);

    public override int Priority => GetPriorityFromDependents([trackRenderingService]);

    protected override Result OnLoad()
    {
        _onTrackAddedQueue
            .SetHandler(OnTrackAdded)
            .Init();
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        _onTrackAddedQueue.Cleanup();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime) => _onTrackAddedQueue.Update();

    private Result OnTrackAdded(Id<Track> trackId)
    {
        trackRenderingService.Unregister(trackId);

        if (GetLineStripData(trackId).TryPickProblems(out var problems, out var data))
        {
            return problems.Prepend("Failed to get line strip data for track");
        }

        if (trackRenderingService.Register(trackId, data).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to register track");
        }

        return Result.Success();
    }

    public Result<LineStripData> GetLineStripData(Id<Track> trackId)
    {
        if (trackSplineService.GetPoints(trackId, TrackVertexCount).TryPickProblems(out var problems, out var positions))
        {
            return problems.Prepend("Failed to get track points");
        }

        if (positions.Length != TrackVertexCount)
        {
            return new ResultProblem("Track length must be equal to vertex count");
        }

        var colors = new Vector3D<float>[TrackVertexCount];
        Array.Fill(colors, new Vector3D<float>(1, 1, 1));

        return new LineStripData
        {
            Positions = positions,
            Colors = colors,
        };
    }
}
