namespace Olve.Engine3D.GUI.Elements;

public static class PairedDictionaryExtensions
{
    public static bool Add<T1, T2>(this (IDictionary<T1, T2> Forward, IDictionary<T2, T1> Backward) dictionaries, T1 key, T2 value)
        where T1 : notnull where T2 : notnull
    {
        var forwardSucceeds = dictionaries.Forward.TryAdd(key, value);
        var backwardSucceeds = dictionaries.Backward.TryAdd(value, key);

        return forwardSucceeds || backwardSucceeds;
    }

    public static bool Remove<T1, T2>(this (IDictionary<T1, T2> Forward, IDictionary<T2, T1> Backward) dictionaries, T1 key)
        where T1 : notnull where T2 : notnull
    {
        if (!dictionaries.Forward.TryGetValue(key, out var value))
        {
            return false;
        }

        var forwardSucceeds = dictionaries.Forward.Remove(key);
        var backwardSucceeds = dictionaries.Backward.Remove(value);

        return forwardSucceeds || backwardSucceeds;
    }
}