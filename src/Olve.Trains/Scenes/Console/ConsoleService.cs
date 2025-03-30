using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Input;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Olve.Trains.Scenes.Console;

public readonly record struct UIDayTime(int Hour, int Minute)
{
    public override string ToString() => $"{Hour:D2}:{Minute:D2}";
}

public readonly record struct UIState(UIDayTime DayTime, IReadOnlyList<string> Console, string? ConsoleCommand = null);

public class ConsoleService(ConsoleCommandService consoleCommandService) : ISceneService
{
    private Thread? _consoleThread;
    private bool _running;
    private UIState _state;

    private bool _consoleActive;
    private readonly StringBuilder _consoleBuffer = new();

    private readonly string[] _consoleMessages = new string[3];

    public Result Load()
    {
        if (_consoleThread is not null || _running)
        {
            return new ResultProblem("Console thread already running");
        }

        _running = true;

        _consoleThread = new Thread(() =>
        {
            var layout = new Layout().SplitRows(
                new Layout("Header").Size(3),
                new Layout("Body"),
                new Layout("Console").Size(6),
                new Layout("Tools").Size(3)
                );

            AnsiConsole.Live(layout).Start(context => Render(context, layout, in _running, in _state));
        });

        _consoleThread.Start();

        Array.Fill(_consoleMessages, string.Empty);

        GameManager.LoggingManager.OnLog += OnLog;

        return Result.Success();
    }

    private void OnLog(LogMessage logMessage)
    {
        var message = logMessage.Message;

        message = logMessage.Level switch {
            LogLevel.Warning => $"[yellow]{message}[/]",
            LogLevel.Error => $"[red]{message}[/]",
            LogLevel.Critical => $"[red bold]{message}[/]",
            _ => message
        };

        _consoleMessages[0] = _consoleMessages[1];
        _consoleMessages[1] = _consoleMessages[2];
        _consoleMessages[2] = message;
    }

    public Result<Pass> Input()
    {
        var keyboardState = GameManager.KeyboardManager.State;

        var shift = keyboardState.IsKeyDown(Key.ShiftLeft) || keyboardState.IsKeyDown(Key.ShiftRight);
        
        if (_consoleActive)
        {
            if (keyboardState.IsKeyPressed(Key.Escape))
            {
                _consoleActive = false;
                _consoleBuffer.Clear();
            }

            if (keyboardState.IsKeyPressed(Key.Enter))
            {
                var result = consoleCommandService.Execute(_consoleBuffer.ToString());
                if (result.TryPickProblems(out var problems))
                {
                    GameManager.LoggingManager.Log(problems);
                }

                _consoleActive = false;
                _consoleBuffer.Clear();
            }

            if (keyboardState.IsKeyPressed(Key.Backspace))
            {
                if (_consoleBuffer.Length > 0)
                {
                    _consoleBuffer.Remove(_consoleBuffer.Length - 1, 1);
                }
            }

            if (keyboardState.IsKeyPressed(Key.Space))
            {
                _consoleBuffer.Append(' ');
            }
            
            if(keyboardState.TryGetPressed(Keys.AlphaNumericKeys, out var key))
            {
                if (key.Value.TryGetChar(out var c, shift))
                {
                    _consoleBuffer.Append(c);
                }
            }
        }
        else
        {
            if (keyboardState.IsKeyPressed(Key.C))
            {
                _consoleActive = true;
                _consoleBuffer.Clear();
            }
        }

        return _consoleActive ? Pass.Block : Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_consoleThread is null || !_consoleThread.IsAlive)
        {
            _consoleThread = null;
            _running = false;
        }

        _state = new UIState(new UIDayTime(
            GameManager.DayTimeManager.CurrentTime.Hours,
            GameManager.DayTimeManager.CurrentTime.Minutes),
            _consoleMessages,
            _consoleActive ? _consoleBuffer.ToString() : null);

        return Result.Success();
    }

    public Result Unload()
    {
        _running = false;

        GameManager.LoggingManager.OnLog -= OnLog;

        return Result.Success();
    }

    private static void Render(LiveDisplayContext context, Layout layout, in bool running, in UIState state)
    {
        UIState previousState = default;

        NoBoxBorder noBoxBorder = new();
        var bold = Style.Parse("bold");
        var dim = Style.Parse("dim");
        var slowBlink = Style.Parse("rapidblink");

        var bodyChanged = true;

        TimeSpan sample0 = new(0), sample1 = new(0), sample2 = new(0), sample3 = new(0), sample4 = new(0);

        while (running)
        {
            if (previousState == state)
            {
                Thread.Sleep(20);
                continue;
            }

            var start = Stopwatch.GetTimestamp();

            var headerChanged = state.DayTime != previousState.DayTime;
            if (headerChanged)
            {
                var refreshTime = (sample0 + sample1 + sample2 + sample3 + sample4).TotalMilliseconds / 5;

                layout["Header"].Update(
                    new Panel(
                        new Align(
                            new Columns(
                                new Text($"Time: {state.DayTime}"),
                                new Text("Balance: 30.000€"),
                                new Text($"UI refresh time: {refreshTime:F2}ms")
                            )
                            {
                                Padding = new Padding(4, 0),
                                Expand = false,
                            },
                        HorizontalAlignment.Center)
                    ).Expand());
            }

            if (bodyChanged)
            {
                var panel = new Panel(string.Empty).Border(noBoxBorder).Expand();

                layout["Body"].Update(panel);

                bodyChanged = false;
            }

            var consoleChanged = state.ConsoleCommand != previousState.ConsoleCommand || !state.Console.CollectionEquals(previousState.Console);
            if (consoleChanged)
            {
                IEnumerable<IRenderable> consoleLines = state.Console.Select(x => new Markup(x));

                var consoleCommandActive = state.ConsoleCommand is not null;
                var consoleCommand = "> " + (state.ConsoleCommand ?? "press [C] to activate console");
                var consoleStyle = consoleCommandActive ? bold : dim;

                if (consoleCommandActive)
                {
                    Columns columns = new(new Text(consoleCommand, consoleStyle), new Text("_", slowBlink))
                    {
                        Padding = new Padding(0, 0, 0, 0),
                        Expand = false
                    };

                    consoleLines = consoleLines.Append(columns);
                }

                else
                {
                    var text = new Text(consoleCommand, consoleStyle);

                    consoleLines = consoleLines.Append(text);
                }

                var rows = new Rows(consoleLines);
                var align = new Align(rows, HorizontalAlignment.Left, VerticalAlignment.Bottom);

                var console = new Panel(align).Header("Console").Expand();

                layout["Console"].Update(console);
            }

            context.Refresh();

            sample4 = sample3;
            sample3 = sample2;
            sample2 = sample1;
            sample1 = sample0;
            sample0 = Stopwatch.GetElapsedTime(start);

            previousState = state;
        }
    }
}

public static class EnumerableExtensions
{
    public static bool CollectionEquals<T>(this IReadOnlyList<T>? first, IReadOnlyList<T>? second)
        where T : notnull
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (!first[i].Equals(second[i]))
            {
                return false;
            }
        }

        return true;
    }
}