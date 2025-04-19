namespace Olve.Engine3D.Rendering;

public class OrderedList<T> where T : IComparable<T>
{
    private readonly List<T> _list = new();

    public int GetIndex(T item)
    {
        var index = _list.BinarySearch(item);
        if (index < 0)
        {
            index = ~index;
        }

        return index;
    }
    
    public T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    public void Insert(T item)
    {
        var index = GetIndex(item);
        if (index >= _list.Count)
        {
            _list.Add(item);
            return;
        }

        var current = _list[index];
        if (current.CompareTo(item) == 0)
        {
            _list[index] = item;
        }

        _list.Insert(index, item);
    }

    public void Remove(T item)
    {
        _list.RemoveAt(GetIndex(item));
    }

    public void Replace(T item)
    {
        var index = GetIndex(item);
        _list[index] = item;
    }

    public T? FirstOrDefault(Func<T, bool> match)
    {
        return _list.FirstOrDefault(match);
    }

    public IEnumerable<T> GetRange(int startIndex, int count)
    {
        for (var i = startIndex; i < startIndex + count; i++)
        {
            yield return _list[i];
        }
    }
}