using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Events;
using Olve.Engine3D.Rendering;
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
    private readonly ConcurrentQueue<PendingScreenshot> _pendingScreenshots = new();
    private readonly ConcurrentQueue<IPath> _pendingFramebufferDumps = new();
    private readonly Dictionary<string, ScreenshotTarget> _targets = new();

    public FramebufferManager? FramebufferManager { get; set; }

    public readonly record struct ScreenshotTarget(
        uint FboHandle, int Width, int Height, bool IsDepth,
        Action<byte[], int, int>? DebugOverlay = null);
    private readonly record struct PendingScreenshot(IPath OutputPath, string? TargetName, bool Debug);

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

    public void RegisterTarget(string name, ScreenshotTarget target)
    {
        _targets[name] = target;
        _logger.LogDebug("Screenshot target registered: {Name}", name);
    }

    public void UnregisterTarget(string name)
    {
        _targets.Remove(name);
    }

    public IReadOnlyCollection<string> TargetNames => _targets.Keys;

    public void RequestScreenshot(IPath outputPath, string? targetName = null, bool debug = false)
    {
        _pendingScreenshots.Enqueue(new PendingScreenshot(outputPath, targetName, debug));
        _logger.LogDebug("Screenshot requested: {Path} (target={Target}, debug={Debug})", outputPath.Path, targetName ?? "screen", debug);
    }

    public void RequestScreenshot(string outputPath, string? targetName = null, bool debug = false) =>
        RequestScreenshot(Path.Create(ExpandTilde(outputPath)), targetName, debug);

    public void RequestFramebufferDump(IPath outputFolder)
    {
        _pendingFramebufferDumps.Enqueue(outputFolder);
        _logger.LogDebug("Framebuffer dump requested: {Path}", outputFolder.Path);
    }

    private static string ExpandTilde(string path) =>
        path.StartsWith("~/")
            ? (Path.GetHomeDirectory() / path[2..]).Path
            : path == "~"
                ? Path.GetHomeDirectory().Path
                : path;

    private void CaptureIfRequested()
    {
        while (_pendingScreenshots.TryDequeue(out var pending))
        {
            if (pending.TargetName is null)
            {
                CaptureScreenshot(pending.OutputPath);
            }
            else if (_targets.TryGetValue(pending.TargetName, out var target))
            {
                CaptureTarget(target, pending.OutputPath, pending.Debug);
            }
            else
            {
                _logger.LogWarning("Unknown screenshot target: {Target}", pending.TargetName);
            }
        }

        while (_pendingFramebufferDumps.TryDequeue(out var outputFolder))
        {
            DumpAllFramebuffers(outputFolder);
        }
    }

    private void CaptureTarget(ScreenshotTarget target, IPath outputPath, bool debug)
    {
        var gl = _glProvider.Value;
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, target.FboHandle);

        if (target.IsDepth)
        {
            CaptureDepthAttachment(gl, target.Width, target.Height, outputPath,
                debug ? target.DebugOverlay : null);
        }
        else
        {
            CaptureColorAttachment(gl, target.Width, target.Height, outputPath);
        }

        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
    }

    private void DumpAllFramebuffers(IPath outputFolder)
    {
        var gl = _glProvider.Value;
        var window = _windowProvider.Value;
        var absoluteFolder = outputFolder.Absolute;
        absoluteFolder.EnsurePathExists();

        // Dump default framebuffer (screen)
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        var screenWidth = window.FramebufferSize.X;
        var screenHeight = window.FramebufferSize.Y;
        CaptureColorAttachment(gl, screenWidth, screenHeight, absoluteFolder / "screen_color.png");
        CaptureDepthAttachment(gl, screenWidth, screenHeight, absoluteFolder / "screen_depth.png");

        // Dump all custom framebuffers
        if (FramebufferManager is not { } framebufferManager) return;
        var framebuffers = framebufferManager.GetAllFramebuffers();
        for (var fbIndex = 0; fbIndex < framebuffers.Count; fbIndex++)
        {
            var fb = framebuffers[fbIndex];
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fb.FboHandle);

            foreach (var attachment in fb.Attachments)
            {
                var name = attachment.IsDepth
                    ? $"fb{fbIndex}_{fb.Width}x{fb.Height}_depth.png"
                    : $"fb{fbIndex}_{fb.Width}x{fb.Height}_color{attachment.Index}.png";

                var path = absoluteFolder / name;

                if (attachment.IsDepth)
                {
                    CaptureDepthAttachment(gl, fb.Width, fb.Height, path);
                }
                else
                {
                    gl.ReadBuffer((ReadBufferMode)((int)ReadBufferMode.ColorAttachment0 + attachment.Index));
                    CaptureColorAttachment(gl, fb.Width, fb.Height, path);
                }
            }
        }

        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        _logger.LogInformation("Framebuffer dump saved to: {Path}", absoluteFolder.Path);
    }

    private void CaptureColorAttachment(GL gl, int width, int height, IPath outputPath)
    {
        var pixelCount = width * height * 4;
        BufferHelper.WithSpan<byte>(pixelCount, pixels =>
        {
            gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
            FlipVertically(pixels, width, height);
            SetAlphaOpaque(pixels);
            WritePng(pixels, width, height, outputPath);
        });
    }

    private void CaptureDepthAttachment(GL gl, int width, int height, IPath outputPath,
        Action<byte[], int, int>? debugOverlay = null)
    {
        var floatCount = width * height;
        BufferHelper.WithSpan<float>(floatCount, depthFloats =>
        {
            gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.DepthComponent, GLEnum.Float, depthFloats);

            // Find min/max for contrast stretching
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var d in depthFloats)
            {
                if (d < min) min = d;
                if (d < 1f && d > max) max = d; // ignore far plane (1.0)
            }

            // Convert to RGBA grayscale
            var pixels = new byte[width * height * 4];
            var range = max - min;
            if (range < 1e-6f) range = 1f;

            for (var i = 0; i < floatCount; i++)
            {
                var normalized = (depthFloats[i] - min) / range;
                var b = (byte)(float.Clamp(normalized, 0f, 1f) * 255f);
                pixels[i * 4 + 0] = b;
                pixels[i * 4 + 1] = b;
                pixels[i * 4 + 2] = b;
                pixels[i * 4 + 3] = 255;
            }

            FlipVertically(pixels, width, height);
            debugOverlay?.Invoke(pixels, width, height);
            WritePng(pixels, width, height, outputPath);
        });
    }

    private void WritePng(Span<byte> pixels, int width, int height, IPath outputPath)
    {
        var absolutePath = outputPath.Absolute;
        absolutePath.Parent.EnsurePathExists();
        using var stream = File.Create(absolutePath.Path);
        var writer = new ImageWriter();
        writer.WritePng(pixels.ToArray(), width, height, ColorComponents.RedGreenBlueAlpha, stream);
        _logger.LogInformation("Saved: {Path}", absolutePath.Path);
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

            // Force alpha to 255 so semi-transparent GUI overlays appear composited
            SetAlphaOpaque(pixels);

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

    private static void SetAlphaOpaque(Span<byte> pixels)
    {
        for (var i = 3; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;
        }
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
