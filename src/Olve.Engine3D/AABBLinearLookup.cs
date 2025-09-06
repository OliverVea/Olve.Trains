using Olve.Engine3D.Math;
using Olve.Utilities.Ids;

namespace Olve.Engine3D;

public sealed class AABBLinearLookup<T> : IAABBLookup<T>
{
    private readonly record struct Entry(Id<AABB> Id, AABB Box, T Data);

    private readonly List<Entry> _entries = [];
    private readonly Dictionary<Id<AABB>, int> _positions = [];

    public int Count => _positions.Count;

    public Id<AABB> Add(AABB box, T data)
    {
        var id = Id<AABB>.New();
        var entry = new Entry { Id = id, Box = box, Data = data };
        _positions.Add(id, _entries.Count);
        _entries.Add(entry);
        return id;
    }

    public bool Remove(Id<AABB> id)
    {
        if (!_positions.TryGetValue(id, out var idx))
        {
            return false;
        }

        var lastIdx = _entries.Count - 1;
        if (idx != lastIdx)
        {
            var last = _entries[lastIdx];
            _entries[idx] = last;
            _positions[last.Id] = idx;
        }

        _entries.RemoveAt(lastIdx);
        _positions.Remove(id);
        return true;
    }

    public bool TryGet(Id<AABB> id, out AABB box, out T data)
    {
        if (_positions.TryGetValue(id, out var idx))
        {
            var e = _entries[idx];
            box = e.Box;
            data = e.Data;
            return true;
        }

        box = default;
        data = default!;
        return false;
    }

    public void Clear()
    {
        _entries.Clear();
        _positions.Clear();
    }

    public IReadOnlyCollection<T> Query(AABB query)
    {
        return _entries.Where(entry => entry.Box.Intersects(query))
            .Select(entry => entry.Data)
            .ToArray();
    }

    public IReadOnlyCollection<T> Query(Vector3D<float> point)
    {
        return _entries.Where(entry => entry.Box.Contains(point))
            .Select(entry => entry.Data)
            .ToArray();
    }
}