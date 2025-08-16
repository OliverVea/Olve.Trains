using Olve.Engine3D.Math.Splines;
using ScottPlot;
using Silk.NET.Maths;

namespace Olve.Engine3D.Tests;

public static class HermiteSplineSandbox
{
    private const float T0 = 0;
    private const float T1 = 1;
    private const float Dt = 0.01f;

    private const int W = 600;
    private const int H = 600;

    const string FileName = "hermite.svg";

    public static void Run1()
    {
        const float c = 1.0f;

        Hermite2.Knot p0 = new(new Vector2D<float>(0, 0), default, new Vector2D<float>(1, 0) * c);
        Hermite2.Knot p1 = new(new Vector2D<float>(1, 1), new Vector2D<float>(0, 1) * c, default);

        KeyFrame<Hermite2.Knot>[] keyFrames =
        [
            new(T0, p0),
            new(T1, p1),
        ];

        Hermite2 spline = new(keyFrames);
        UniformHermite<Vector2D<float>> uniformSpline = new(spline, new Vector2Metric());

        List<float> xs = [], ys = [];
        List<float> xsU = [], ysU = [];

        for (float t = T0; t < T1; t += Dt)
        {
            var result = spline.Sample(t);

            xs.Add(result.X);
            ys.Add(result.Y);

            result = uniformSpline.SampleAtDistance(t * uniformSpline.Length);

            xsU.Add(result.X + 0.2f);
            ysU.Add(result.Y);
        }

        Plot myPlot = new();

        var scatter = myPlot.Add.Scatter(xs, ys);
        scatter.MarkerSize *= 0.4f;
        
        scatter = myPlot.Add.Scatter(xsU, ysU);
        scatter.MarkerSize *= 0.4f;

        if (!Paths.Path.TryGetAssemblyExecutable(out var path))
        {
            throw new Exception("Could not get assembly executable path");
        }

        var filePath = path.Parent / FileName;

        myPlot.SaveSvg(filePath.Path, W, H);

        var linkString = filePath.GetLinkString();
        Console.WriteLine($"Plot saved to: {linkString}");
    }

    public static void RunMultipleCs()
    {
        float[] cValues = [0.5f, 1.0f, 1.5f, 2.0f, 3.0f];

        Plot plot = new();

        foreach (float c in cValues)
        {
            Hermite2.Knot p0 = new(new Vector2D<float>(0, 0), default, new Vector2D<float>(1, 0) * c);
            Hermite2.Knot p1 = new(new Vector2D<float>(1, 1), new Vector2D<float>(0, 1) * c, default);

            KeyFrame<Hermite2.Knot>[] keyFrames =
            [
                new(T0, p0),
                new(T1, p1),
            ];

            Hermite2 spline = new(keyFrames);

            List<float> xs = [], ys = [];

            for (float t = T0; t < T1; t += Dt)
            {
                var result = spline.Sample(t);
                xs.Add(result.X);
                ys.Add(result.Y);
            }

            var scatter = plot.Add.Scatter(xs, ys);
            scatter.LegendText = $"C = {c}";
            scatter.MarkerSize *= 0.4f;
        }

        plot.ShowLegend();

        if (!Paths.Path.TryGetAssemblyExecutable(out var path))
        {
            throw new Exception("Could not get assembly executable path");
        }

        var filePath = path.Parent / FileName;

        plot.SaveSvg(filePath.Path, W, H);

        var linkString = filePath.GetLinkString();
        Console.WriteLine($"Plot saved to: {linkString}");
    }

}