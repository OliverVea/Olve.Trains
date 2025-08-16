namespace Olve.Engine3D.Tests;

using Math.Splines;
using ScottPlot;
using Silk.NET.Maths;

public static class UniformHermiteSplineSandbox
{
    private const float T0 = 0;
    private const float T1 = 1;
    private const int   W  = 900;
    private const int   H  = 600;

    const string DistFile = "hermite_distance_distributions.svg";

    public static void RunDistanceDistributions()
    {
        // Curve setup (same shape as your examples)
        const float cTangent = 1.0f;
        var p0 = new Hermite2.Knot(new Vector2D<float>(0, 0), default, new Vector2D<float>(1, 0) * cTangent);
        var p1 = new Hermite2.Knot(new Vector2D<float>(1, 1), new Vector2D<float>(0, 1) * cTangent, default);

        KeyFrame<Hermite2.Knot>[] keyFrames =
        [
            new(T0, p0),
            new(T1, p1),
        ];

        var spline = new Hermite2(keyFrames);

        // How many points to sample along the whole curve
        const int n = 100; // adjust if you like
        const float tStep = (T1 - T0) / (n - 1);

        // --- 1) Distances for NON-uniform (equal-t) sampling
        var ptsNonUniform = new Vector2D<float>[n];
        for (var i = 0; i < n; i++)
        {
            var t = T0 + i * tStep;
            ptsNonUniform[i] = spline.Sample(t);
        }
        var distsNonUniform = PairwiseDistances(ptsNonUniform);

        // --- 2) Distances for UNIFORM sampling with different c (samples-per-segment)
        int[] cValues = [16, 32, 64, 128, 256];
        var series = new List<(string name, float[] distances)> { ("non-uniform", distsNonUniform) };

        foreach (var c in cValues)
        {
            var uniform = new UniformHermite<Vector2D<float>>(spline, new Vector2Metric(), samplesPerSegment: c);
            var pts = uniform.SampleUniform(n).ToArray();
            var d = PairwiseDistances(pts);
            series.Add(($"c={c}", d));
        }

        // --- Print summary stats
        Console.WriteLine("Distance distribution stats (N points -> N-1 distances)");
        Console.WriteLine("Series         \tMin\tMax\tMean\tStd\tCV");
        foreach (var (name, d) in series)
        {
            var (min, max, mean, std, cv) = Stats(d);
            Console.WriteLine($"{name,-14}\t{min:F6}\t{max:F6}\t{mean:F6}\t{std:F6}\t{cv:P2}");
        }

        // --- Plot CDF-style curves (sorted distance vs percentile)
        var plot = new Plot();

        foreach (var (name, d) in series)
        {
            //Array.Sort(d);
            var m = d.Length;
            var xs = new double[m];
            var ys = new double[m];
            for (var i = 0; i < m; i++)
            {
                xs[i] = (double)i / (m - 1); // percentile 0..1
                ys[i] = d[i];
            }

            var curve = plot.Add.Scatter(xs, ys);
            curve.LegendText = name;
            curve.LineWidth = name == "non-uniform" ? 2f : 1.5f;
            curve.MarkerSize = 0;
        }

        plot.Title("Hermite: distance distribution (CDF)");
        plot.XLabel("Percentile");
        plot.YLabel("Consecutive point distance");
        plot.ShowLegend();

        if (!Paths.Path.TryGetAssemblyExecutable(out var path))
            throw new Exception("Could not get assembly executable path");

        var filePath = path.Parent / DistFile;
        plot.SaveSvg(filePath.Path, W, H);

        Console.WriteLine($"Plot saved to: {filePath.GetLinkString()}");
    }

    // --- Helpers ---

    private static float[] PairwiseDistances(IReadOnlyList<Vector2D<float>> pts)
    {
        var n = pts.Count;
        var d = new float[n - 1];
        for (var i = 0; i < n - 1; i++)
            d[i] = Dist(pts[i], pts[i + 1]);
        return d;
    }

    private static float Dist(in Vector2D<float> a, in Vector2D<float> b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static (float min, float max, float mean, float std, float cv) Stats(float[] values)
    {
        var n = values.Length;
        if (n == 0) return (0, 0, 0, 0, 0);

        float min = float.MaxValue, max = float.MinValue, sum = 0f;
        for (var i = 0; i < n; i++)
        {
            var v = values[i];
            if (v < min) min = v;
            if (v > max) max = v;
            sum += v;
        }
        var mean = sum / n;

        var var = 0f;
        for (var i = 0; i < n; i++)
        {
            var dv = values[i] - mean;
            var += dv * dv;
        }
        var /= int.Max(1, n - 1);
        var std = MathF.Sqrt(var);
        var cv = mean != 0f ? std / mean : 0f;
        return (min, max, mean, std, cv);
    }
}
