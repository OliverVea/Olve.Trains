namespace Olve.Engine3D.Math.Splines;

public sealed class UniformHermite<T> : IInterpolator<T>
{
    private readonly Hermite<T> _curve;

    private readonly float[] _segStartLen;
    private readonly float[][] _cumLen;
    private readonly float[][] _ts;

    public UniformHermite(Hermite<T> curve, IArcLengthMetric<T> metric, int samplesPerSegment = 64)
    {
        _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        var metric1 = metric ?? throw new ArgumentNullException(nameof(metric));
        var samplesPerSegment1 = int.Max(2, samplesPerSegment);

        var segmentCount = curve.SegmentCount - 1;

        _segStartLen = new float[segmentCount];
        _cumLen = new float[segmentCount][];
        _ts = new float[segmentCount][];

        var run = 0f;
        for (var si = 0; si < segmentCount; si++)
        {
            _segStartLen[si] = run;

            var K = samplesPerSegment1;
            var tArr = new float[K];
            var lenArr = new float[K];

            var prev = _curve.EvaluateLocal(si, 0f);
            tArr[0] = 0f;
            lenArr[0] = 0f;

            for (var k = 1; k < K; k++)
            {
                var t = (float)k / (K - 1);
                var curr = _curve.EvaluateLocal(si, t);
                lenArr[k] = lenArr[k - 1] + metric1.Distance(prev, curr);
                tArr[k] = t;
                prev = curr;
            }

            _ts[si] = tArr;
            _cumLen[si] = lenArr;
            run += lenArr[^1];
        }

        Length = run;
    }

    public float Length { get; }

    public T SampleAtDistance(float s)
    {
        s = float.Clamp(s, 0, Length);
        var seg = FindSegment(s);
        var sLocal = s - _segStartLen[seg];
        var tLocal = FindTByLocalDistance(seg, sLocal);
        return _curve.EvaluateLocal(seg, tLocal);
    }

    public IReadOnlyList<T> SampleUniform(int count)
    {
        if (count <= 0) return [];
        if (count == 1) return [SampleAtDistance(0)];

        var arr = new T[count];
        var step = Length / (count - 1);
        for (var i = 0; i < count; i++)
            arr[i] = SampleAtDistance(i * step);
        return arr;
    }

    private int FindSegment(float s)
    {
        var last = _segStartLen.Length - 1;

        for (var i = 0; i < last; i++)
        {
            if (s < _segStartLen[i + 1]) return i;
        }

        return last;
    }

    private float FindTByLocalDistance(int seg, float sLocal)
    {
        var len = _cumLen[seg];
        var ts = _ts[seg];

        if (sLocal <= 0) return 0f;
        var segLen = len[^1];
        if (sLocal >= segLen || segLen <= 1e-6f) return 1f;

        // binary search the LUT
        int lo = 0, hi = len.Length - 1;
        while (lo + 1 < hi)
        {
            var mid = (lo + hi) >> 1;
            if (len[mid] < sLocal) lo = mid; else hi = mid;
        }

        // linear interpolate t
        float l0 = len[lo], l1 = len[hi];
        var u = (sLocal - l0) / MathF.Max(1e-6f, (l1 - l0));
        return ts[lo] + u * (ts[hi] - ts[lo]);
    }

    public T Min { get => _curve.Min; set => _curve.Min = value; }
    public T Max { get => _curve.Max; set => _curve.Max = value; }
    public T Sample(float time)
    {
        var t = float.Clamp(time, 0f, 1f);
        var s = t * Length;
        return SampleAtDistance(s);
    }

    public T Tangent(float time)
    {
        var t = float.Clamp(time, 0f, 1f);
        var s = t * Length;

        var seg = FindSegment(s);
        var sLocal = s - _segStartLen[seg];
        var tLocal = FindTByLocalDistance(seg, sLocal);

        return _curve.TangentLocal(seg, tLocal);
    }
}
