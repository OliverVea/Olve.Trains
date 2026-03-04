using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.Physics3D.Collisions;

public class HeightmapRaycaster
{
    private readonly HeightmapData _heightmapData;
    private readonly float _epsilon;
    private readonly int _min;
    private readonly int _max;

    public HeightmapRaycaster(HeightmapData heightmapData, float epsilon = 0.01f)
    {
        _heightmapData = heightmapData;
        _epsilon = epsilon;

        _min = int.MaxValue;
        _max = int.MinValue;

        foreach (var height in heightmapData.Heights)
        {
            if (height < _min) _min = height;
            if (height > _max) _max = height;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="raycastHit"></param>
    /// <exception cref="ArgumentException">If the <c>ray.Direction.Y</c> is 0 or if the </exception>
    /// <returns></returns>
    public bool TryRaycast(Ray3D<float> ray, [NotNullWhen(true)] out Vector3D<float>? raycastHit)
    {
        raycastHit = null;
        try
        {
            if (float.Abs(ray.Direction.Y) < _epsilon) throw new ArgumentException("Input was invalid");

            float top = _max * _heightmapData.Step, bottom = _min * _heightmapData.Step;

            while (bottom <= top)
            {
                var mid = (top + bottom) / 2;
                if (!ray.TryEvaluateWithY(mid, out var point))
                {
                    return false;
                }

                if (point.X < 0 || point.Z < 0 || point.X > _heightmapData.Width - 1 || point.Z > _heightmapData.Length - 1)
                {
                    return false;
                }

                var i = (int)float.Clamp(point.X + 0.5f, 0, _heightmapData.Width - 1);
                var j = (int)float.Clamp(point.Z + 0.5f, 0, _heightmapData.Length - 1);

                var height = _heightmapData.Heights[j * _heightmapData.Width + i] * _heightmapData.Step;

                if (float.Abs(mid - height) < _epsilon + _heightmapData.Step)
                {
                    // Snap Y to actual terrain height (mid is an approximation from binary search)
                    raycastHit = point with { Y = height };
                    return true;
                }
                if (mid < height)
                {
                    bottom = mid + _heightmapData.Step;
                }
                else
                {
                    top = mid - _heightmapData.Step;
                }
            }

        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }
}