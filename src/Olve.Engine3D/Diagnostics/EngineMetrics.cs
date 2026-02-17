using System.Diagnostics.Metrics;

namespace Olve.Engine3D.Diagnostics;

public static class EngineMetrics
{
    public static readonly Meter Meter = new("Olve.Engine3D");

    public static readonly Histogram<double> FrameDuration =
        Meter.CreateHistogram<double>("engine.frame.duration", "ms", "Total frame duration");

    public static readonly Histogram<double> UpdateDuration =
        Meter.CreateHistogram<double>("engine.update.duration", "ms", "Update phase duration");

    public static readonly Histogram<double> RenderDuration =
        Meter.CreateHistogram<double>("engine.render.duration", "ms", "Render phase duration");

    public static readonly Histogram<double> InputDuration =
        Meter.CreateHistogram<double>("engine.input.duration", "ms", "Input phase duration");

    public static readonly UpDownCounter<int> ActiveScenes =
        Meter.CreateUpDownCounter<int>("engine.scenes.active", description: "Number of active scenes");
}
