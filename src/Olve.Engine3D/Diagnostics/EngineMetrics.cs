using System.Diagnostics.Metrics;

namespace Olve.Engine3D.Diagnostics;

public static class EngineMetrics
{
    public static readonly Meter Meter = new("Olve.Engine3D");

    /// <summary>
    /// Set to true by the host application when a metrics listener (e.g. OTel MeterProvider)
    /// is configured. When false, <see cref="GameManager"/> skips Stopwatch timing to avoid
    /// unnecessary overhead.
    /// </summary>
    public static bool IsEnabled { get; set; }

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

    public static readonly Counter<long> CommandsProcessed =
        Meter.CreateCounter<long>("engine.commands.processed", description: "Total commands processed");

    public static readonly Histogram<double> SceneUpdateDuration =
        Meter.CreateHistogram<double>("engine.scene.update.duration", "ms", "Per-scene update duration");

    public static readonly Histogram<double> SceneRenderDuration =
        Meter.CreateHistogram<double>("engine.scene.render.duration", "ms", "Per-scene render duration");
}
