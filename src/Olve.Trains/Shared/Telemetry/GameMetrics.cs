using System.Diagnostics.Metrics;

namespace Olve.Trains.Shared.Telemetry;

public static class GameMetrics
{
    public static readonly Meter Meter = new("Olve.Trains");

    public static readonly UpDownCounter<int> TrackCount =
        Meter.CreateUpDownCounter<int>("game.tracks.count", description: "Number of tracks");

    public static readonly UpDownCounter<int> VehicleCount =
        Meter.CreateUpDownCounter<int>("game.vehicles.count", description: "Number of vehicles");

    public static readonly UpDownCounter<int> BuildingCount =
        Meter.CreateUpDownCounter<int>("game.buildings.count", description: "Number of buildings");
}
