using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Instancing;

internal sealed class InstanceStore<TInstance> : IInstanceStore where TInstance : IInstanceData
{
    private readonly Dictionary<Id<TInstance>, TInstance> _instances = new();

    public int Count => _instances.Count;

    public void Add(Id<TInstance> id, TInstance data) => _instances[id] = data;

    public bool Update(Id<TInstance> id, TInstance data)
    {
        if (!_instances.ContainsKey(id)) return false;
        _instances[id] = data;
        return true;
    }

    public bool Remove(Id<TInstance> id) => _instances.Remove(id);

    public float[] MarshalToFloats()
    {
        if (_instances.Count == 0) return [];

        var floats = new float[_instances.Count * TInstance.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var instance in _instances.Values)
        {
            instance.WriteTo(span.Slice(offset, TInstance.FloatCount));
            offset += TInstance.FloatCount;
        }

        return floats;
    }
}
