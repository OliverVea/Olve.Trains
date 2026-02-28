using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Events;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageWriteSharp;

namespace Olve.Engine3D;

public class ScreenshotManager
{
    private readonly Provider<GL> _glProvider;
    private readonly Provider<IWindow> _windowProvider;
    private readonly ILogger<ScreenshotManager> _logger;
    private readonly ConcurrentQueue<IPath> _pendingScreenshots = new();

    public ScreenshotManager(
        Provider<GL> glProvider,
        Provider<IWindow> windowProvider,
        AfterRenderEvent afterRenderEvent,
        ILogger<ScreenshotManager> logger)
    {
        _glProvider = glProvider;
        _windowProvider = windowProvider;
        _logger = logger;

        afterRenderEvent.AfterRender.Subscribe(CaptureIfRequested);
    }

    public void RequestScreenshot(IPath outputPath)
    {
        _pendingScreenshots.Enqueue(outputPath);
        _logger.LogDebug("Screenshot requested: {Path}", outputPath.Path);
    }

    public void RequestScreenshot(string outputPath) =>
        RequestScreenshot(Path.Create(ExpandTilde(outputPath)));

    private static string ExpandTilde(string path) =>
        path.StartsWith("~/")
            ? (Path.GetHomeDirectory() / path[2..]).Path
            : path == "~"
                ? Path.GetHomeDirectory().Path
                : path;

    private void CaptureIfRequested()
    {
        while (_pendingScreenshots.TryDequeue(out var outputPath))
        {
            CaptureScreenshot(outputPath);
        }
    }

    private void CaptureScreenshot(IPath outputPath)
    {
        var gl = _glProvider.Value;
        var window = _windowProvider.Value;

        var width = window.FramebufferSize.X;
        var height = window.FramebufferSize.Y;
        var pixelCount = width * height * 4; // RGBA

        BufferHelper.WithSpan<byte>(pixelCount, pixels =>
        {
            gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);

            // OpenGL reads pixels bottom-up, need to flip vertically
            FlipVertically(pixels, width, height);

            // Ensure parent directory exists
            var absolutePath = outputPath.Absolute;
            absolutePath.Parent.EnsurePathExists();

            // Write PNG
            using var stream = File.Create(absolutePath.Path);
            var writer = new ImageWriter();
            writer.WritePng(pixels.ToArray(), width, height, ColorComponents.RedGreenBlueAlpha, stream);

            _logger.LogInformation("Screenshot saved: {Path}", absolutePath.Path);
        });
    }

    private static void FlipVertically(Span<byte> pixels, int width, int height)
    {
        var rowSize = width * 4;
        Span<byte> tempRow = stackalloc byte[rowSize];

        for (var y = 0; y < height / 2; y++)
        {
            var topRow = pixels.Slice(y * rowSize, rowSize);
            var bottomRow = pixels.Slice((height - 1 - y) * rowSize, rowSize);

            topRow.CopyTo(tempRow);
            bottomRow.CopyTo(topRow);
            tempRow.CopyTo(bottomRow);
        }
    }
}
