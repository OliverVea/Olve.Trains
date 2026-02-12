namespace Olve.Trains.Scenes.GameLogic.Junctions;

public class RoundRobin
{
    private int _count;

    public int Sample(int n)
    {
        _count++;
        return (_count - 1) % n;
    }

    public T Sample<T>(IReadOnlyList<T> elements)
    {
        var index = Sample(elements.Count);
        return elements[index];
    }
}