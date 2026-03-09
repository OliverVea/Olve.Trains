using System.Diagnostics.Metrics;

namespace Olve.Trains.Shared.Telemetry;

public static class GameMetrics
{
    public static readonly Meter Meter = new("Olve.Trains");

    public static readonly UpDownCounter<int> TrackCount =
        Meter.CreateUpDownCounter<int>("game.tracks.count", description: "Number of tracks");

    public static readonly UpDownCounter<int> TrainCount =
        Meter.CreateUpDownCounter<int>("game.trains.count", description: "Number of trains");

    public static readonly UpDownCounter<int> BuildingCount =
        Meter.CreateUpDownCounter<int>("game.buildings.count", description: "Number of buildings");
}
