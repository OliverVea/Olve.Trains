using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Ordering;

/// <summary>An entity that records when it was created; see <see cref="SequenceService"/>.</summary>
public interface IHasCreatedSequence
{
    long CreatedSequence { get; }
}

/// <summary>
/// How to order entities. Store enumeration order is arbitrary and differs between runs, so anything whose
/// outcome depends on order sorts explicitly:
/// <list type="bullet">
/// <item>Order makes no difference: don't sort.</item>
/// <item>Needs a stable order (simulation, saves, command output): <see cref="Of{T}"/>, sequence then id.</item>
/// <item>User-facing lists: a meaningful field first (e.g. name), then sequence, then id.</item>
/// </list>
/// </summary>
public static class CreationOrder
{
    /// <summary>Orders by creation sequence, then by id.</summary>
    public static IComparer<T> Of<T>() where T : IHasId<Id<T>>, IHasCreatedSequence => Comparer<T>.Instance;

    private static class Comparer<T> where T : IHasId<Id<T>>, IHasCreatedSequence
    {
        public static readonly IComparer<T> Instance = System.Collections.Generic.Comparer<T>.Create((a, b) =>
        {
            var bySequence = a.CreatedSequence.CompareTo(b.CreatedSequence);
            return bySequence != 0 ? bySequence : a.Id.CompareTo(b.Id);
        });
    }
}
