using Olve.Engine3D.Math;
using Olve.Utilities.Ids;

namespace Olve.Engine3D;

/// <summary>
/// Lookup structure for AABBs with attached data. Entries are referenced by Id&lt;AABB&gt;.
/// </summary>
public interface IAABBLookup<T>
{
    /// <summary>Total number of stored entries.</summary>
    int Count { get; }

    /// <summary>
    /// Adds a box with attached data and returns a stable identifier for later operations.
    /// </summary>
    Id<AABB> Add(AABB box, T data);

    /// <summary>
    /// Removes the specific entry identified by <paramref name="id"/>.
    /// Returns true if the id existed and was removed.
    /// </summary>
    bool Remove(Id<AABB> id);

    /// <summary>
    /// Attempts to retrieve the current (box, data) pair for <paramref name="id"/>.
    /// </summary>
    bool TryGet(Id<AABB> id, out AABB box, out T data);

    /// <summary>Removes all entries.</summary>
    void Clear();

    /// <summary>
    /// Returns the attached data for every box that intersects <paramref name="query"/>.
    /// Intersection should be inclusive for touching faces/edges.
    /// </summary>
    IReadOnlyCollection<T> Query(AABB query);

    /// <summary>
    /// Returns the attached data for every box that contains <paramref name="point"/>.
    /// Containment should be inclusive on boundaries.
    /// </summary>
    IReadOnlyCollection<T> Query(Vector3D<float> point);
}