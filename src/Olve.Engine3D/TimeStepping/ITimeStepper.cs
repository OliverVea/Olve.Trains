using Olve.Engine3D.Events;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Olve.Engine3D.TimeStepping;

public interface ITimeStepper : IDisposable
{
    Event Load { get; }
    // TODO: Remove deltaTime from Render — render services don't use it currently.
    // If sim/render rates are decoupled later, accumulate sim deltas for the render delta.
    Event<TimeSpan> Render { get; }
    Event<TimeSpan> Update { get; }
    Event Closing { get; }
    void Run();
}

public class WindowStepper(Provider<IWindow> windowProvider, Provider<GL> glProvider, ScreenResizedEvent screenResizedEvent, GameClosingEvent gameClosingEvent) : ITimeStepper
{
    public void Dispose()
    {
    }

    public Event Load { get; } = new();
    public Event<TimeSpan> Render { get; } = new();
    public Event<TimeSpan> Update { get; } = new();
    public Event Closing { get; } = new();

    public void Run()
    {
        windowProvider.Value.Load += Load.Invoke;
        windowProvider.Value.Update += dt => Update.Invoke(TimeSpan.FromSeconds(dt));
        windowProvider.Value.Render += dt => Render.Invoke(TimeSpan.FromSeconds(dt));
        windowProvider.Value.Closing += Closing.Invoke;
        windowProvider.Value.FramebufferResize += size =>
        {
            glProvider.Value.Viewport(size);
            screenResizedEvent.OnWindowResize.Invoke(size);
        };

        gameClosingEvent.GameClosing.Subscribe(windowProvider.Value.Close);

        windowProvider.Value.Run();

        windowProvider.Dispose();
    }
}

public class ManualStepper(GameClosingEvent gameClosingEvent) : ITimeStepper
{
    // TODO: Support other frame rates
    private const float FrameTimeSeconds = 1 / 60f;
    private static readonly TimeSpan FrameTime = TimeSpan.FromSeconds(FrameTimeSeconds);
    private static readonly TimeSpan SamplePeriod = TimeSpan.FromMilliseconds(20);

    public Event Load { get; } = new();
    public Event<TimeSpan> Render { get; } = new();
    public Event<TimeSpan> Update { get; } = new();
    public Event Closing { get; } = new();

    private ulong _currentFrames;
    private ulong _desiredFrames;

    private bool _shouldRun;

    public void Dispose()
    {

    }

    public void Run()
    {
        Load.Invoke();
        gameClosingEvent.GameClosing.Subscribe(() => _shouldRun = false);

        _shouldRun = true;
        while (_shouldRun)
        {
            var stepped = false;
            for (; _currentFrames < _desiredFrames; _currentFrames++)
            {
                Update.Invoke(FrameTime);
                stepped = true;
            }

            if (stepped)
            {
                Render.Invoke(FrameTime);
            }

            Thread.Sleep(SamplePeriod);
        }
    }

    public void Step(ulong frames) => _desiredFrames += frames;

}