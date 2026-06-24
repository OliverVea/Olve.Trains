namespace Olve.Trains.Saves;

/// <summary>
/// Versioned, JSON-serializable snapshot of root game state.
/// </summary>
/// <remarks>
/// These are serialization models, deliberately decoupled from the runtime domain types: the wire
/// format only changes when a model here changes, never as a silent side effect of a domain refactor.
/// State is materialized, never regenerated from a seed — seeds are new-game inputs, not save state.
/// Derived entities (junctions, stations, depots, industries, cities, resources) are omitted; the event
/// system rebuilds them on load. Track, building, train, and signal-rule data are added in later steps.
/// </remarks>
public sealed record SaveFile
{
    /// <summary>Schema version, read before the rest of the document so older saves can be upgraded forward.</summary>
    public int Version { get; init; } = SaveFileSchema.CurrentVersion;

    /// <summary>Player balance.</summary>
    public required int Money { get; init; }

    /// <summary>Time-of-day clock state.</summary>
    public required SaveTime Time { get; init; }

    /// <summary>Terrain heightmap.</summary>
    public required SaveTerrain Terrain { get; init; }

    /// <summary>Placed environmental objects (trees and the like).</summary>
    public required SaveEnvironment Environment { get; init; }

    /// <summary>Camera state.</summary>
    public required SaveCamera Camera { get; init; }
}

/// <summary>Time-of-day clock state.</summary>
public sealed record SaveTime
{
    /// <summary>Time of day the game starts at, in hours within [0, 24).</summary>
    public required float DayStartHours { get; init; }

    /// <summary>Real-world duration of one in-game day, in seconds.</summary>
    public required double DayDurationSeconds { get; init; }

    /// <summary>Total elapsed in-game time, in game-hours — fully defines the current day and time of day.</summary>
    public required double CurrentGameHours { get; init; }
}

/// <summary>Terrain heightmap. A serialization twin of the domain heightmap, not a reference to it.</summary>
public sealed record SaveTerrain
{
    /// <summary>X-axis dimension in tiles.</summary>
    public required int Width { get; init; }

    /// <summary>Z-axis dimension in tiles.</summary>
    public required int Length { get; init; }

    /// <summary>Vertical size of one discrete height step.</summary>
    public required float Step { get; init; }

    /// <summary>Row-major height values, length <see cref="Width"/> × <see cref="Length"/>.</summary>
    public required int[] Heights { get; init; }
}

/// <summary>Placed environmental objects.</summary>
public sealed record SaveEnvironment
{
    /// <summary>Every placed object, materialized — positions are player intent and never regenerated.</summary>
    public required IReadOnlyList<SaveEnvironmentalObject> Objects { get; init; }
}

/// <summary>A single placed environmental object (root entity).</summary>
public sealed record SaveEnvironmentalObject
{
    /// <summary>Entity id (Guid string).</summary>
    public required string Id { get; init; }

    /// <summary>Blueprint id the object was placed from (Guid string).</summary>
    public required string BlueprintId { get; init; }

    /// <summary>World position.</summary>
    public required SaveVector3 Position { get; init; }

    /// <summary>World rotation.</summary>
    public required SaveQuaternion Rotation { get; init; }
}

/// <summary>Camera state.</summary>
public sealed record SaveCamera
{
    /// <summary>Orthographic zoom level.</summary>
    public required float OrthographicSize { get; init; }
}

/// <summary>A 3D vector. Serialization twin of the domain vector type.</summary>
public sealed record SaveVector3
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
}

/// <summary>A rotation quaternion. Serialization twin of the domain quaternion type.</summary>
public sealed record SaveQuaternion
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
    public required float W { get; init; }
}
