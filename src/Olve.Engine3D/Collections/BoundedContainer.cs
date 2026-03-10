using System.Collections.Immutable;

namespace Olve.Engine3D.Collections;

/// <summary>
/// A capacity-limited container that tracks integer amounts per key,
/// with optional per-key limits.
/// </summary>
public class BoundedContainer<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, int> _amounts = new();

    public int Capacity { get; }
    public ImmutableDictionary<TKey, int>? PerKeyLimits { get; }

    public BoundedContainer(int capacity, ImmutableDictionary<TKey, int>? perKeyLimits = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Capacity = capacity;
        PerKeyLimits = perKeyLimits;
    }

    public int GetAmount(TKey key)
    {
        return _amounts.GetValueOrDefault(key);
    }

    public int GetTotalAmount()
    {
        var total = 0;
        foreach (var amount in _amounts.Values) total += amount;
        return total;
    }

    public int GetRemainingCapacity()
    {
        return Capacity - GetTotalAmount();
    }

    public int GetRemainingCapacityForKey(TKey key)
    {
        var remainingTotal = GetRemainingCapacity();

        if (PerKeyLimits is null) return remainingTotal;
        if (!PerKeyLimits.TryGetValue(key, out var perKeyMax)) return 0;

        var currentOfKey = GetAmount(key);
        var remainingForKey = perKeyMax - currentOfKey;
        return System.Math.Min(remainingTotal, remainingForKey);
    }

    public bool CanAccept(TKey key)
    {
        return PerKeyLimits is null || PerKeyLimits.ContainsKey(key);
    }

    public bool TryUpdateExact(TKey key, int delta)
    {
        if (delta == 0) return true;

        if (delta > 0)
        {
            if (!CanAccept(key)) return false;
            if (GetRemainingCapacityForKey(key) < delta) return false;

            _amounts.TryGetValue(key, out var current);
            _amounts[key] = current + delta;
        }
        else
        {
            _amounts.TryGetValue(key, out var current);
            if (current + delta < 0) return false;

            var newAmount = current + delta;
            if (newAmount == 0)
                _amounts.Remove(key);
            else
                _amounts[key] = newAmount;
        }

        return true;
    }

    public int UpdateWithinCapacity(TKey key, int delta)
    {
        if (delta == 0) return 0;

        if (delta > 0)
        {
            if (!CanAccept(key)) return 0;
            var maxAdd = GetRemainingCapacityForKey(key);
            var actualDelta = System.Math.Min(delta, maxAdd);
            if (actualDelta <= 0) return 0;

            _amounts.TryGetValue(key, out var current);
            _amounts[key] = current + actualDelta;
            return actualDelta;
        }
        else
        {
            _amounts.TryGetValue(key, out var current);
            var actualDelta = System.Math.Max(delta, -current);
            if (actualDelta == 0) return 0;

            var newAmount = current + actualDelta;
            if (newAmount == 0)
                _amounts.Remove(key);
            else
                _amounts[key] = newAmount;
            return actualDelta;
        }
    }

    public IEnumerable<(TKey Key, int Amount)> GetEntries()
    {
        foreach (var (key, amount) in _amounts)
        {
            yield return (key, amount);
        }
    }
}
